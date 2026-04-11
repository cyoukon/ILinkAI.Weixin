package com.ilinkai.weixin.media;

import com.ilinkai.weixin.cdn.CdnClient;
import com.ilinkai.weixin.cdn.CdnException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.UUID;

/** 媒体下载服务 */
public class MediaDownloadService {
    private static final Logger logger = LoggerFactory.getLogger(MediaDownloadService.class);
    public static final int MAX_MEDIA_BYTES = 100 * 1024 * 1024;

    private final CdnClient cdnClient;
    private final String cdnBaseUrl;

    public MediaDownloadService(String cdnBaseUrl) {
        this.cdnBaseUrl = cdnBaseUrl;
        this.cdnClient = new CdnClient();
    }

    public byte[] downloadImage(String encryptQueryParam, String aesKeyBase64) throws CdnException {
        if (encryptQueryParam == null || encryptQueryParam.isEmpty())
            throw new IllegalArgumentException("encryptQueryParam is required");
        if (aesKeyBase64 != null && !aesKeyBase64.isEmpty()) {
            return cdnClient.downloadAndDecrypt(encryptQueryParam, aesKeyBase64, cdnBaseUrl, "DownloadImage");
        } else {
            return cdnClient.downloadPlain(encryptQueryParam, cdnBaseUrl, "DownloadImage-plain");
        }
    }

    public byte[] downloadVoice(String encryptQueryParam, String aesKeyBase64) throws CdnException {
        if (encryptQueryParam == null || aesKeyBase64 == null || encryptQueryParam.isEmpty() || aesKeyBase64.isEmpty())
            throw new IllegalArgumentException("encryptQueryParam and aesKeyBase64 are required");
        return cdnClient.downloadAndDecrypt(encryptQueryParam, aesKeyBase64, cdnBaseUrl, "DownloadVoice");
    }

    public byte[] downloadFile(String encryptQueryParam, String aesKeyBase64) throws CdnException {
        if (encryptQueryParam == null || aesKeyBase64 == null || encryptQueryParam.isEmpty() || aesKeyBase64.isEmpty())
            throw new IllegalArgumentException("encryptQueryParam and aesKeyBase64 are required");
        return cdnClient.downloadAndDecrypt(encryptQueryParam, aesKeyBase64, cdnBaseUrl, "DownloadFile");
    }

    public byte[] downloadVideo(String encryptQueryParam, String aesKeyBase64) throws CdnException {
        if (encryptQueryParam == null || aesKeyBase64 == null || encryptQueryParam.isEmpty() || aesKeyBase64.isEmpty())
            throw new IllegalArgumentException("encryptQueryParam and aesKeyBase64 are required");
        return cdnClient.downloadAndDecrypt(encryptQueryParam, aesKeyBase64, cdnBaseUrl, "DownloadVideo");
    }

    public String saveMediaToFile(byte[] data, String directory, String fileName) throws IOException {
        new File(directory).mkdirs();
        if (fileName == null || fileName.isEmpty()) {
            fileName = "media_" + LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"))
                    + "_" + UUID.randomUUID().toString().replace("-", "") + ".bin";
        }
        String filePath = directory + File.separator + fileName;
        try (FileOutputStream fos = new FileOutputStream(filePath)) {
            fos.write(data);
        }
        logger.debug("SaveMediaToFile: saved to {}", filePath);
        return filePath;
    }
}
