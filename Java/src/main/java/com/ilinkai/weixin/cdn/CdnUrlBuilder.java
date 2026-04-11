package com.ilinkai.weixin.cdn;

import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;

/** CDN URL构建工具 */
public class CdnUrlBuilder {

    public static String buildDownloadUrl(String encryptedQueryParam, String cdnBaseUrl) {
        String baseUrl = cdnBaseUrl.endsWith("/") ? cdnBaseUrl : cdnBaseUrl + "/";
        return baseUrl + "download?encrypted_query_param=" + urlEncode(encryptedQueryParam);
    }

    public static String buildUploadUrl(String uploadParam, String fileKey, String cdnBaseUrl) {
        String baseUrl = cdnBaseUrl.endsWith("/") ? cdnBaseUrl : cdnBaseUrl + "/";
        return baseUrl + "upload?encrypted_query_param=" + urlEncode(uploadParam) + "&filekey=" + urlEncode(fileKey);
    }

    private static String urlEncode(String value) {
        try {
            return URLEncoder.encode(value, StandardCharsets.UTF_8.name());
        } catch (Exception e) {
            return value;
        }
    }
}
