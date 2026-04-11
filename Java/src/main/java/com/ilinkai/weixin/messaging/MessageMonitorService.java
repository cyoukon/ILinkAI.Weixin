package com.ilinkai.weixin.messaging;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ilinkai.weixin.*;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.auth.SessionGuard;
import com.ilinkai.weixin.models.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;
import java.util.stream.Collectors;

/** 消息监控服务 - 长轮询获取微信消息 */
public class MessageMonitorService {
    private static final Logger logger = LoggerFactory.getLogger(MessageMonitorService.class);
    private static final ObjectMapper MAPPER = new ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

    private static final int MAX_CONSECUTIVE_FAILURES = 3;
    private static final int BACKOFF_DELAY_MS = 30_000;
    private static final int RETRY_DELAY_MS = 2_000;

    private final MessageMonitorOptions options;
    private final WeixinApiClient apiClient;
    private final SessionGuard sessionGuard;
    private final MessageProcessor messageProcessor;
    private final String syncBufFilePath;
    private volatile boolean running;

    private Consumer<MessageContext> onMessageReceived;
    private Consumer<Exception> onError;

    public MessageMonitorService(MessageMonitorOptions options) {
        this.options = options;
        WeixinApiClientOptions clientOpts = new WeixinApiClientOptions();
        clientOpts.setBaseUrl(options.baseUrl);
        clientOpts.setCdnBaseUrl(options.cdnBaseUrl);
        clientOpts.setToken(options.token);
        clientOpts.setLongPollTimeoutMs(options.longPollTimeoutMs);
        this.apiClient = new WeixinApiClient(clientOpts);
        this.sessionGuard = new SessionGuard();
        this.messageProcessor = new MessageProcessor(options.cdnBaseUrl);

        String stateDir = getStateDir();
        new File(stateDir).mkdirs();
        this.syncBufFilePath = stateDir + File.separator + AccountStore.normalizeAccountId(options.accountId) + "_sync_buf.json";
    }

    public void setOnMessageReceived(Consumer<MessageContext> handler) { this.onMessageReceived = handler; }
    public void setOnError(Consumer<Exception> handler) { this.onError = handler; }

    private static String getStateDir() {
        String envPath = System.getenv("ILINKAI_STATE_DIR");
        if (envPath != null && !envPath.trim().isEmpty()) return envPath;
        return System.getProperty("user.home") + File.separator + ".ilinkai" + File.separator + "sync";
    }

    private String loadSyncBuf() {
        try {
            if (!new File(syncBufFilePath).exists()) return "";
            String raw = new String(Files.readAllBytes(Paths.get(syncBufFilePath)));
            SyncBufData data = MAPPER.readValue(raw, SyncBufData.class);
            return data != null && data.getUpdatesBuf != null ? data.getUpdatesBuf : "";
        } catch (Exception e) { return ""; }
    }

    private void saveSyncBuf(String syncBuf) {
        try {
            SyncBufData data = new SyncBufData();
            data.getUpdatesBuf = syncBuf;
            Files.write(Paths.get(syncBufFilePath), MAPPER.writeValueAsBytes(data));
        } catch (Exception e) { logger.error("Failed to save sync buf", e); }
    }

    public void start() throws InterruptedException {
        running = true;
        logger.info("Monitor started: baseUrl={} accountId={}", options.baseUrl, options.accountId);

        String syncBuf = loadSyncBuf();
        int consecutiveFailures = 0;

        while (running) {
            try {
                sessionGuard.assertSessionActive(options.accountId);
                ApiModels.GetUpdatesResponse response = apiClient.getUpdates(syncBuf);

                boolean isApiError = (response.getRet() != null && response.getRet() != 0)
                        || (response.getErrCode() != null && response.getErrCode() != 0);

                if (isApiError) {
                    boolean isSessionExpired = (response.getErrCode() != null && response.getErrCode() == SessionGuard.SESSION_EXPIRED_ERROR_CODE)
                            || (response.getRet() != null && response.getRet() == SessionGuard.SESSION_EXPIRED_ERROR_CODE);
                    if (isSessionExpired) {
                        sessionGuard.pauseSession(options.accountId);
                        long pauseMs = sessionGuard.getRemainingPauseMs(options.accountId);
                        logger.error("getUpdates: session expired, pausing for {} min", Math.ceil(pauseMs / 60000.0));
                        if (onError != null) onError.accept(new WeixinSessionPausedException(options.accountId, (int) Math.ceil(pauseMs / 60000.0)));
                        consecutiveFailures = 0;
                        Thread.sleep(pauseMs);
                        continue;
                    }
                    consecutiveFailures++;
                    if (consecutiveFailures >= MAX_CONSECUTIVE_FAILURES) {
                        consecutiveFailures = 0;
                        Thread.sleep(BACKOFF_DELAY_MS);
                    } else {
                        Thread.sleep(RETRY_DELAY_MS);
                    }
                    continue;
                }

                consecutiveFailures = 0;
                if (response.getGetUpdatesBuf() != null && !response.getGetUpdatesBuf().isEmpty()) {
                    saveSyncBuf(response.getGetUpdatesBuf());
                    syncBuf = response.getGetUpdatesBuf();
                }

                List<WeixinMessage> messages = response.getMessages() != null ? response.getMessages() : new ArrayList<>();
                for (WeixinMessage msg : messages) {
                    MessageContext context = messageProcessor.convertToContext(msg, options.accountId);
                    if (options.mediaDir != null && !options.mediaDir.isEmpty()) {
                        messageProcessor.processMedia(context, options.mediaDir);
                    }
                    if (onMessageReceived != null) onMessageReceived.accept(context);
                }
            } catch (WeixinSessionPausedException e) {
                throw new RuntimeException(e);
            } catch (InterruptedException e) {
                break;
            } catch (Exception err) {
                consecutiveFailures++;
                logger.error("getUpdates error ({}/{})", consecutiveFailures, MAX_CONSECUTIVE_FAILURES, err);
                if (onError != null) onError.accept(err);
                if (consecutiveFailures >= MAX_CONSECUTIVE_FAILURES) {
                    consecutiveFailures = 0;
                    Thread.sleep(BACKOFF_DELAY_MS);
                } else {
                    Thread.sleep(RETRY_DELAY_MS);
                }
            }
        }
        logger.info("Monitor ended");
    }

    public void stop() { running = false; }

    private static class SyncBufData {
        @JsonProperty("get_updates_buf") public String getUpdatesBuf;
    }

    public static class MessageMonitorOptions {
        public String baseUrl = WeixinApiClient.DEFAULT_BASE_URL;
        public String cdnBaseUrl = WeixinApiClient.DEFAULT_CDN_BASE_URL;
        public String token;
        public String accountId = "";
        public int longPollTimeoutMs = 35_000;
        public String mediaDir;
    }
}
