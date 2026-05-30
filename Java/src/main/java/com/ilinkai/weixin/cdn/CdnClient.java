package com.ilinkai.weixin.cdn;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Base64;

/** CDN客户端服务 - 处理文件的上传和下载 */
public class CdnClient {
    private static final Logger logger = LoggerFactory.getLogger(CdnClient.class);
    private static final int MAX_RETRIES = 3;

    /** 上传缓冲区到CDN（带AES-128-ECB加密） */
    public String uploadBuffer(byte[] buffer, String uploadParam, String fileKey,
                               String cdnBaseUrl, byte[] aesKey, String label) throws CdnException {
        String cdnUrl = CdnUrlBuilder.buildUploadUrl(uploadParam, fileKey, cdnBaseUrl);
        return uploadBufferCore(buffer, cdnUrl, aesKey, label);
    }

    /** 上传缓冲区到CDN（使用完整URL） */
    public String uploadBufferByUrl(byte[] buffer, String uploadFullUrl,
                                     byte[] aesKey, String label) throws CdnException {
        return uploadBufferCore(buffer, uploadFullUrl, aesKey, label);
    }

    private String uploadBufferCore(byte[] buffer, String cdnUrl, byte[] aesKey, String label) throws CdnException {
        byte[] ciphertext;
        try {
            ciphertext = AesEcbCrypto.encrypt(buffer, aesKey);
        } catch (Exception e) {
            throw new CdnException("AES encryption failed: " + e.getMessage());
        }
        logger.debug("{}: CDN POST url={} ciphertextSize={}", label, redactUrl(cdnUrl), ciphertext.length);

        Exception lastError = null;
        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++) {
            try {
                HttpURLConnection conn = (HttpURLConnection) new URL(cdnUrl).openConnection();
                conn.setRequestMethod("POST");
                conn.setDoOutput(true);
                conn.setRequestProperty("Content-Type", "application/octet-stream");

                try (OutputStream os = conn.getOutputStream()) {
                    os.write(ciphertext);
                }

                int status = conn.getResponseCode();

                if (status >= 400 && status < 500) {
                    String errMsg = conn.getHeaderField("x-error-message");
                    if (errMsg == null) errMsg = readStream(conn.getErrorStream());
                    throw new CdnException("CDN upload client error " + status + ": " + errMsg, status);
                }

                if (status < 200 || status >= 300) {
                    String errMsg = conn.getHeaderField("x-error-message");
                    if (errMsg == null) errMsg = "status " + status;
                    throw new CdnException("CDN upload server error: " + errMsg, status);
                }

                String downloadParam = conn.getHeaderField("x-encrypted-param");
                if (downloadParam == null || downloadParam.isEmpty()) {
                    throw new CdnException("CDN upload response missing x-encrypted-param header");
                }

                logger.debug("{}: CDN upload success attempt={}", label, attempt);
                return downloadParam;

            } catch (CdnException e) {
                throw e;
            } catch (Exception e) {
                lastError = e;
                if (attempt < MAX_RETRIES) {
                    logger.error("{}: attempt {} failed, retrying... err={}", label, attempt, e.getMessage());
                }
            }
        }
        throw new CdnException("CDN upload failed after " + MAX_RETRIES + " attempts: " +
                (lastError != null ? lastError.getMessage() : "unknown"));
    }

    /** 从CDN下载并解密数据 */
    public byte[] downloadAndDecrypt(String encryptedQueryParam, String aesKeyBase64,
                                      String cdnBaseUrl, String label) throws CdnException {
        byte[] key = parseAesKey(aesKeyBase64, label);
        String url = CdnUrlBuilder.buildDownloadUrl(encryptedQueryParam, cdnBaseUrl);
        logger.debug("{}: fetching url={}", label, redactUrl(url));

        byte[] encrypted = fetchCdnBytes(url, label);
        logger.debug("{}: downloaded {} bytes, decrypting", label, encrypted.length);

        try {
            byte[] decrypted = AesEcbCrypto.decrypt(encrypted, key);
            logger.debug("{}: decrypted {} bytes", label, decrypted.length);
            return decrypted;
        } catch (Exception e) {
            throw new CdnException(label + ": AES decryption failed: " + e.getMessage());
        }
    }

    /** 从CDN下载原始数据（不解密） */
    public byte[] downloadPlain(String encryptedQueryParam, String cdnBaseUrl, String label) throws CdnException {
        String url = CdnUrlBuilder.buildDownloadUrl(encryptedQueryParam, cdnBaseUrl);
        logger.debug("{}: fetching url={}", label, redactUrl(url));
        return fetchCdnBytes(url, label);
    }

    private byte[] fetchCdnBytes(String url, String label) throws CdnException {
        try {
            HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
            conn.setRequestMethod("GET");
            int status = conn.getResponseCode();

            if (status < 200 || status >= 300) {
                String body = readStream(conn.getErrorStream());
                throw new CdnException(label + ": CDN download " + status + " " + conn.getResponseMessage() + " body=" + body, status);
            }

            return readBytes(conn.getInputStream());
        } catch (CdnException e) {
            throw e;
        } catch (Exception e) {
            throw new CdnException(label + ": fetch network error: " + e.getMessage());
        }
    }

    private static byte[] parseAesKey(String aesKeyBase64, String label) throws CdnException {
        byte[] decoded = Base64.getDecoder().decode(aesKeyBase64);
        if (decoded.length == 16) return decoded;
        if (decoded.length == 32) {
            String hexString = new String(decoded, StandardCharsets.US_ASCII);
            if (isHexString(hexString)) {
                return hexStringToBytes(hexString);
            }
        }
        throw new CdnException(label + ": aes_key must decode to 16 raw bytes or 32-char hex string, got " + decoded.length + " bytes");
    }

    private static boolean isHexString(String s) {
        if (s.length() != 32) return false;
        for (char c : s.toCharArray()) {
            if ("0123456789abcdefABCDEF".indexOf(c) < 0) return false;
        }
        return true;
    }

    static byte[] hexStringToBytes(String hex) {
        byte[] result = new byte[hex.length() / 2];
        for (int i = 0; i < result.length; i++) {
            result[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
        }
        return result;
    }

    static String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) sb.append(String.format("%02x", b));
        return sb.toString();
    }

    private static String readStream(InputStream is) throws IOException {
        if (is == null) return "";
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        int n;
        while ((n = is.read(buf)) != -1) baos.write(buf, 0, n);
        return baos.toString(StandardCharsets.UTF_8.name());
    }

    private static byte[] readBytes(InputStream is) throws IOException {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        byte[] buf = new byte[8192];
        int n;
        while ((n = is.read(buf)) != -1) baos.write(buf, 0, n);
        return baos.toByteArray();
    }

    private static String redactUrl(String url) {
        int idx = url.indexOf('?');
        return idx >= 0 ? url.substring(0, idx) + "?[REDACTED]" : url;
    }
}
