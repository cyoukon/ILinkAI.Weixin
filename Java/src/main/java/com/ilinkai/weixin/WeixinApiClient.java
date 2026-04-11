package com.ilinkai.weixin;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ilinkai.weixin.models.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.ArrayList;
import java.util.Base64;

/**
 * ILinkai微信API客户端
 * 提供与ilinkai.weixin.qq.com API交互的核心功能
 */
public class WeixinApiClient {
    private static final Logger logger = LoggerFactory.getLogger(WeixinApiClient.class);

    public static final String DEFAULT_BASE_URL = "https://ilinkai.weixin.qq.com";
    public static final String DEFAULT_CDN_BASE_URL = "https://novac2c.cdn.weixin.qq.com/c2c";
    public static final int SESSION_EXPIRED_ERROR_CODE = -14;

    private final WeixinApiClientOptions options;
    private final String channelVersion;
    static final ObjectMapper MAPPER = new ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

    public WeixinApiClient() {
        this(null);
    }

    public WeixinApiClient(WeixinApiClientOptions options) {
        this.options = options != null ? options : new WeixinApiClientOptions();
        this.channelVersion = getChannelVersionInternal();
    }

    private static String getChannelVersionInternal() {
        Package pkg = WeixinApiClient.class.getPackage();
        if (pkg != null && pkg.getImplementationVersion() != null) {
            return pkg.getImplementationVersion();
        }
        return "1.0.0";
    }

    private BaseInfo buildBaseInfo() {
        BaseInfo info = new BaseInfo();
        info.setChannelVersion(channelVersion);
        return info;
    }

    private static String generateRandomWechatUin() {
        byte[] randomBytes = new byte[4];
        new SecureRandom().nextBytes(randomBytes);
        long uint32Value = ((long)(randomBytes[0] & 0xFF)) |
                ((long)(randomBytes[1] & 0xFF) << 8) |
                ((long)(randomBytes[2] & 0xFF) << 16) |
                ((long)(randomBytes[3] & 0xFF) << 24);
        String decimalString = String.valueOf(uint32Value & 0xFFFFFFFFL);
        return Base64.getEncoder().encodeToString(decimalString.getBytes(StandardCharsets.UTF_8));
    }

    private static String ensureTrailingSlash(String url) {
        return url.endsWith("/") ? url : url + "/";
    }

    private String apiFetch(String endpoint, Object body, int timeoutMs, String label) throws WeixinApiException, IOException {
        String baseUrl = ensureTrailingSlash(options.getBaseUrl());
        String url = baseUrl + endpoint;
        String jsonBody = MAPPER.writeValueAsString(body);

        logger.debug("POST {} body={}", redactUrl(url), redactBody(jsonBody));

        HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
        conn.setRequestMethod("POST");
        conn.setDoOutput(true);
        conn.setConnectTimeout(timeoutMs);
        conn.setReadTimeout(timeoutMs);
        conn.setRequestProperty("Content-Type", "application/json");
        conn.setRequestProperty("AuthorizationType", "ilink_bot_token");
        conn.setRequestProperty("X-WECHAT-UIN", generateRandomWechatUin());

        byte[] bodyBytes = jsonBody.getBytes(StandardCharsets.UTF_8);
        conn.setRequestProperty("Content-Length", String.valueOf(bodyBytes.length));

        String token = options.getToken();
        if (token != null && !token.trim().isEmpty()) {
            conn.setRequestProperty("Authorization", "Bearer " + token.trim());
        }
        String routeTag = options.getRouteTag();
        if (routeTag != null && !routeTag.trim().isEmpty()) {
            conn.setRequestProperty("SKRouteTag", routeTag);
        }

        try (OutputStream os = conn.getOutputStream()) {
            os.write(bodyBytes);
        }

        int status = conn.getResponseCode();
        String rawText;
        try (InputStream is = status >= 400 ? conn.getErrorStream() : conn.getInputStream()) {
            rawText = readStream(is);
        }

        logger.debug("{} status={} raw={}", label, status, redactBody(rawText));

        if (status < 200 || status >= 300) {
            throw new WeixinApiException(label + " " + status + ": " + rawText, status);
        }
        return rawText;
    }

    private static String readStream(InputStream is) throws IOException {
        if (is == null) return "";
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        int n;
        while ((n = is.read(buf)) != -1) {
            baos.write(buf, 0, n);
        }
        return baos.toString(StandardCharsets.UTF_8.name());
    }

    /** 获取更新（长轮询） */
    public ApiModels.GetUpdatesResponse getUpdates(String getUpdatesBuf) throws WeixinApiException, IOException {
        ApiModels.GetUpdatesRequest request = new ApiModels.GetUpdatesRequest();
        request.setGetUpdatesBuf(getUpdatesBuf != null ? getUpdatesBuf : "");
        request.setBaseInfo(buildBaseInfo());
        try {
            String rawText = apiFetch("ilink/bot/getupdates", request, options.getLongPollTimeoutMs(), "getUpdates");
            ApiModels.GetUpdatesResponse resp = MAPPER.readValue(rawText, ApiModels.GetUpdatesResponse.class);
            return resp != null ? resp : new ApiModels.GetUpdatesResponse();
        } catch (java.net.SocketTimeoutException e) {
            logger.debug("getUpdates: client-side timeout after {}ms, returning empty response", options.getLongPollTimeoutMs());
            ApiModels.GetUpdatesResponse resp = new ApiModels.GetUpdatesResponse();
            resp.setRet(0);
            resp.setMessages(new ArrayList<>());
            resp.setGetUpdatesBuf(getUpdatesBuf);
            return resp;
        }
    }

    /** 获取上传URL */
    public ApiModels.GetUploadUrlResponse getUploadUrl(ApiModels.GetUploadUrlRequest request) throws WeixinApiException, IOException {
        request.setBaseInfo(buildBaseInfo());
        String rawText = apiFetch("ilink/bot/getuploadurl", request, options.getTimeoutMs(), "getUploadUrl");
        ApiModels.GetUploadUrlResponse resp = MAPPER.readValue(rawText, ApiModels.GetUploadUrlResponse.class);
        return resp != null ? resp : new ApiModels.GetUploadUrlResponse();
    }

    /** 发送消息 */
    public void sendMessage(ApiModels.SendMessageRequest request) throws WeixinApiException, IOException {
        apiFetch("ilink/bot/sendmessage", request, options.getTimeoutMs(), "sendMessage");
    }

    /** 获取配置 */
    public ApiModels.GetConfigResponse getConfig(String ilinkUserId, String contextToken) throws WeixinApiException, IOException {
        ApiModels.GetConfigRequest request = new ApiModels.GetConfigRequest();
        request.setILinkUserId(ilinkUserId);
        request.setContextToken(contextToken);
        request.setBaseInfo(buildBaseInfo());
        String rawText = apiFetch("ilink/bot/getconfig", request, options.getConfigTimeoutMs(), "getConfig");
        ApiModels.GetConfigResponse resp = MAPPER.readValue(rawText, ApiModels.GetConfigResponse.class);
        return resp != null ? resp : new ApiModels.GetConfigResponse();
    }

    /** 发送输入状态 */
    public void sendTyping(ApiModels.SendTypingRequest request) throws WeixinApiException, IOException {
        request.setBaseInfo(buildBaseInfo());
        apiFetch("ilink/bot/sendtyping", request, options.getConfigTimeoutMs(), "sendTyping");
    }

    public WeixinApiClientOptions getOptions() { return options; }

    private static String redactUrl(String url) {
        int idx = url.indexOf('?');
        return idx >= 0 ? url.substring(0, idx) + "?[REDACTED]" : url;
    }

    private static String redactBody(String body) {
        if (body == null || body.length() < 200) return body;
        return body.substring(0, 200) + "...[TRUNCATED]";
    }
}
