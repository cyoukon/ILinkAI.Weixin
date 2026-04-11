namespace ILinkai.Weixin.Sdk.Cdn;

/// <summary>
/// CDN URL构建工具
/// 用于构建微信CDN上传和下载URL
/// </summary>
public static class CdnUrlBuilder
{
    /// <summary>
    /// 构建CDN下载URL
    /// </summary>
    /// <param name="encryptedQueryParam">加密查询参数</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <returns>完整的下载URL</returns>
    public static string BuildDownloadUrl(string encryptedQueryParam, string cdnBaseUrl)
    {
        var baseUrl = cdnBaseUrl.EndsWith("/") ? cdnBaseUrl : $"{cdnBaseUrl}/";
        return $"{baseUrl}download?encrypted_query_param={Uri.EscapeDataString(encryptedQueryParam)}";
    }

    /// <summary>
    /// 构建CDN上传URL
    /// </summary>
    /// <param name="uploadParam">上传参数</param>
    /// <param name="fileKey">文件键</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <returns>完整的上传URL</returns>
    public static string BuildUploadUrl(string uploadParam, string fileKey, string cdnBaseUrl)
    {
        var baseUrl = cdnBaseUrl.EndsWith("/") ? cdnBaseUrl : $"{cdnBaseUrl}/";
        return $"{baseUrl}upload?encrypted_query_param={Uri.EscapeDataString(uploadParam)}&filekey={Uri.EscapeDataString(fileKey)}";
    }
}
