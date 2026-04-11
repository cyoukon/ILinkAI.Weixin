package com.ilinkai.weixin.media;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiException;
import com.ilinkai.weixin.cdn.AesEcbCrypto;
import com.ilinkai.weixin.cdn.CdnClient;
import com.ilinkai.weixin.cdn.CdnException;
import com.ilinkai.weixin.models.ApiModels;
import com.ilinkai.weixin.models.Enums;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.security.SecureRandom;

/** 媒体上传服务 */
public class MediaUploadService {
    private static final Logger logger = LoggerFactory.getLogger(MediaUploadService.class);

    private final WeixinApiClient apiClient;
    private final CdnClient cdnClient;
    private final String cdnBaseUrl;

    public MediaUploadService(WeixinApiClient apiClient, String cdnBaseUrl) {
        this.apiClient = apiClient;
        this.cdnBaseUrl = cdnBaseUrl;
        this.cdnClient = new CdnClient();
    }

    public UploadedFileInfo uploadFile(String filePath, String toUserId, int mediaType)
            throws IOException, WeixinApiException, CdnException {
        byte[] plaintext = Files.readAllBytes(Paths.get(filePath));
        int rawSize = plaintext.length;
        String rawFileMd5 = md5Hex(plaintext);
        int fileSize = AesEcbCrypto.getPaddedSize(rawSize);
        String fileKey = generateFileKey();
        byte[] aesKey = AesEcbCrypto.generateKey();

        logger.debug("UploadFile: file={} rawSize={} fileSize={} md5={} fileKey={}",
                filePath, rawSize, fileSize, rawFileMd5, fileKey);

        ApiModels.GetUploadUrlRequest request = new ApiModels.GetUploadUrlRequest();
        request.setFileKey(fileKey);
        request.setMediaType(mediaType);
        request.setToUserId(toUserId);
        request.setRawSize(rawSize);
        request.setRawFileMd5(rawFileMd5);
        request.setFileSize(fileSize);
        request.setNoNeedThumb(true);
        request.setAesKey(bytesToHex(aesKey));

        ApiModels.GetUploadUrlResponse uploadUrlResp = apiClient.getUploadUrl(request);
        String uploadParam = uploadUrlResp.getUploadParam();
        if (uploadParam == null || uploadParam.isEmpty()) {
            throw new IOException("getUploadUrl returned no upload_param");
        }

        String downloadParam = cdnClient.uploadBuffer(plaintext, uploadParam, fileKey, cdnBaseUrl, aesKey,
                "UploadFile[fileKey=" + fileKey + "]");

        UploadedFileInfo info = new UploadedFileInfo();
        info.fileKey = fileKey;
        info.downloadEncryptedQueryParam = downloadParam;
        info.aesKey = bytesToHex(aesKey);
        info.fileSize = rawSize;
        info.fileSizeCiphertext = fileSize;
        return info;
    }

    public UploadedFileInfo uploadImage(String filePath, String toUserId) throws IOException, WeixinApiException, CdnException {
        return uploadFile(filePath, toUserId, Enums.UploadMediaType.IMAGE);
    }

    public UploadedFileInfo uploadVideo(String filePath, String toUserId) throws IOException, WeixinApiException, CdnException {
        return uploadFile(filePath, toUserId, Enums.UploadMediaType.VIDEO);
    }

    public UploadedFileInfo uploadFileAttachment(String filePath, String toUserId) throws IOException, WeixinApiException, CdnException {
        return uploadFile(filePath, toUserId, Enums.UploadMediaType.FILE);
    }

    private static String generateFileKey() {
        byte[] bytes = new byte[16];
        new SecureRandom().nextBytes(bytes);
        return bytesToHex(bytes);
    }

    private static String md5Hex(byte[] data) {
        try {
            byte[] digest = MessageDigest.getInstance("MD5").digest(data);
            return bytesToHex(digest);
        } catch (Exception e) {
            throw new RuntimeException("MD5 not available", e);
        }
    }

    private static String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) sb.append(String.format("%02x", b));
        return sb.toString();
    }

    public static class UploadedFileInfo {
        public String fileKey = "";
        public String downloadEncryptedQueryParam = "";
        public String aesKey = "";
        public int fileSize;
        public int fileSizeCiphertext;
    }
}
