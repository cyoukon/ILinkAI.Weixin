package com.ilinkai.weixin.auth;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiException;
import com.ilinkai.weixin.models.ApiModels;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import java.util.function.Consumer;

/** 二维码登录服务 */
public class QRCodeLoginService {
    private static final Logger logger = LoggerFactory.getLogger(QRCodeLoginService.class);
    private static final ObjectMapper MAPPER = new ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

    private static final int QR_LONG_POLL_TIMEOUT_MS = 35_000;
    private static final int MAX_QR_REFRESH_COUNT = 3;
    public static final String DEFAULT_ILINK_BOT_TYPE = "3";

    private final String apiBaseUrl;
    private final String routeTag;

    public QRCodeLoginService(String apiBaseUrl, String routeTag) {
        this.apiBaseUrl = apiBaseUrl != null ? apiBaseUrl : WeixinApiClient.DEFAULT_BASE_URL;
        this.routeTag = routeTag;
    }

    public QRCodeLoginService(String apiBaseUrl) { this(apiBaseUrl, null); }

    /** 获取二维码 */
    public ApiModels.GetQRCodeResponse fetchQRCode(String botType) throws WeixinApiException, IOException {
        if (botType == null) botType = DEFAULT_ILINK_BOT_TYPE;
        String baseUrl = apiBaseUrl.endsWith("/") ? apiBaseUrl : apiBaseUrl + "/";
        String url = baseUrl + "ilink/bot/get_bot_qrcode?bot_type=" + URLEncoder.encode(botType, "UTF-8");

        logger.info("Fetching QR code from: {}", url);

        HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(QR_LONG_POLL_TIMEOUT_MS);
        conn.setReadTimeout(QR_LONG_POLL_TIMEOUT_MS);
        if (routeTag != null && !routeTag.trim().isEmpty()) {
            conn.setRequestProperty("SKRouteTag", routeTag);
        }

        int status = conn.getResponseCode();
        String rawText = readStream(status >= 400 ? conn.getErrorStream() : conn.getInputStream());

        if (status < 200 || status >= 300) {
            throw new WeixinApiException("Failed to fetch QR code: " + status + " " + conn.getResponseMessage(), status);
        }

        ApiModels.GetQRCodeResponse resp = MAPPER.readValue(rawText, ApiModels.GetQRCodeResponse.class);
        return resp != null ? resp : new ApiModels.GetQRCodeResponse();
    }

    /** 轮询二维码状态 */
    public ApiModels.GetQRCodeStatusResponse pollQRStatus(String qrcode) throws WeixinApiException, IOException {
        String baseUrl = apiBaseUrl.endsWith("/") ? apiBaseUrl : apiBaseUrl + "/";
        String url = baseUrl + "ilink/bot/get_qrcode_status?qrcode=" + URLEncoder.encode(qrcode, "UTF-8");

        HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(QR_LONG_POLL_TIMEOUT_MS);
        conn.setReadTimeout(QR_LONG_POLL_TIMEOUT_MS);
        conn.setRequestProperty("iLink-App-ClientVersion", "1");
        if (routeTag != null && !routeTag.trim().isEmpty()) {
            conn.setRequestProperty("SKRouteTag", routeTag);
        }

        try {
            int status = conn.getResponseCode();
            String rawText = readStream(status >= 400 ? conn.getErrorStream() : conn.getInputStream());

            if (status < 200 || status >= 300) {
                throw new WeixinApiException("Failed to poll QR status: " + status, status);
            }

            ApiModels.GetQRCodeStatusResponse resp = MAPPER.readValue(rawText, ApiModels.GetQRCodeStatusResponse.class);
            return resp != null ? resp : new ApiModels.GetQRCodeStatusResponse();
        } catch (java.net.SocketTimeoutException e) {
            logger.debug("pollQRStatus: client-side timeout, returning wait");
            ApiModels.GetQRCodeStatusResponse resp = new ApiModels.GetQRCodeStatusResponse();
            resp.setStatus("wait");
            return resp;
        }
    }

    /** 开始二维码登录 */
    public QRLoginStartResult startLogin(String accountId, String botType) {
        String sessionKey = accountId != null ? accountId : UUID.randomUUID().toString();
        if (botType == null) botType = DEFAULT_ILINK_BOT_TYPE;

        logger.info("Starting Weixin login with bot_type={}", botType);
        try {
            ApiModels.GetQRCodeResponse qrResponse = fetchQRCode(botType);
            QRLoginStartResult result = new QRLoginStartResult();
            result.sessionKey = sessionKey;
            result.qrCode = qrResponse.getQrCode() != null ? qrResponse.getQrCode() : "";
            result.qrCodeUrl = qrResponse.getQrCodeImgContent();
            result.message = "使用微信扫描以下二维码，以完成连接。";
            return result;
        } catch (Exception err) {
            logger.error("Failed to start Weixin login", err);
            QRLoginStartResult result = new QRLoginStartResult();
            result.sessionKey = sessionKey;
            result.message = "Failed to start login: " + err.getMessage();
            return result;
        }
    }

    /** 等待登录完成 */
    public QRLoginWaitResult waitForLogin(String qrcode, int timeoutMs,
                                           Consumer<String> onStatusChanged,
                                           Consumer<String> onQRRefreshed) throws InterruptedException {
        long deadline = System.currentTimeMillis() + timeoutMs;
        int qrRefreshCount = 1;
        String currentQRCode = qrcode;
        boolean scannedPrinted = false;

        logger.info("Starting to poll QR code status...");

        while (System.currentTimeMillis() < deadline) {
            try {
                ApiModels.GetQRCodeStatusResponse statusResponse = pollQRStatus(currentQRCode);
                String status = statusResponse.getStatus() != null ? statusResponse.getStatus().toLowerCase() : "";

                switch (status) {
                    case "wait":
                        if (onStatusChanged != null) onStatusChanged.accept(".");
                        break;
                    case "scaned":
                        if (!scannedPrinted) {
                            if (onStatusChanged != null) onStatusChanged.accept("\n👀 已扫码，在微信继续操作...\n");
                            scannedPrinted = true;
                        }
                        break;
                    case "expired":
                        qrRefreshCount++;
                        if (qrRefreshCount > MAX_QR_REFRESH_COUNT) {
                            QRLoginWaitResult r = new QRLoginWaitResult();
                            r.connected = false;
                            r.message = "登录超时：二维码多次过期，请重新开始登录流程。";
                            return r;
                        }
                        if (onStatusChanged != null)
                            onStatusChanged.accept("\n⏳ 二维码已过期，正在刷新...(" + qrRefreshCount + "/" + MAX_QR_REFRESH_COUNT + ")\n");
                        try {
                            ApiModels.GetQRCodeResponse newQR = fetchQRCode(null);
                            currentQRCode = newQR.getQrCode() != null ? newQR.getQrCode() : "";
                            scannedPrinted = false;
                            if (onQRRefreshed != null)
                                onQRRefreshed.accept(newQR.getQrCodeImgContent() != null ? newQR.getQrCodeImgContent() : "");
                        } catch (Exception refreshErr) {
                            QRLoginWaitResult r = new QRLoginWaitResult();
                            r.connected = false;
                            r.message = "刷新二维码失败: " + refreshErr.getMessage();
                            return r;
                        }
                        break;
                    case "confirmed":
                        if (statusResponse.getILinkBotId() == null || statusResponse.getILinkBotId().isEmpty()) {
                            QRLoginWaitResult r = new QRLoginWaitResult();
                            r.connected = false;
                            r.message = "登录失败：服务器未返回 ilink_bot_id。";
                            return r;
                        }
                        logger.info("✅ Login confirmed! ilink_bot_id={}", statusResponse.getILinkBotId());
                        QRLoginWaitResult r = new QRLoginWaitResult();
                        r.connected = true;
                        r.botToken = statusResponse.getBotToken();
                        r.accountId = statusResponse.getILinkBotId();
                        r.baseUrl = statusResponse.getBaseUrl();
                        r.userId = statusResponse.getILinkUserId();
                        r.message = "✅ 与微信连接成功！";
                        return r;
                }
            } catch (Exception err) {
                logger.error("Error polling QR status", err);
                QRLoginWaitResult r = new QRLoginWaitResult();
                r.connected = false;
                r.message = "Login failed: " + err.getMessage();
                return r;
            }
            Thread.sleep(1000);
        }

        QRLoginWaitResult r = new QRLoginWaitResult();
        r.connected = false;
        r.message = "登录超时，请重试。";
        return r;
    }

    private static String readStream(InputStream is) throws IOException {
        if (is == null) return "";
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        int n;
        while ((n = is.read(buf)) != -1) baos.write(buf, 0, n);
        return baos.toString(StandardCharsets.UTF_8.name());
    }

    public static class QRLoginStartResult {
        public String sessionKey = "";
        public String qrCode = "";
        public String qrCodeUrl;
        public String message = "";
    }

    public static class QRLoginWaitResult {
        public boolean connected;
        public String botToken;
        public String accountId;
        public String baseUrl;
        public String userId;
        public String message = "";
    }
}
