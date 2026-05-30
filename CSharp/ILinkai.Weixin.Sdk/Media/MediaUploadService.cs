using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk.Cdn;

namespace ILinkai.Weixin.Sdk.Media;

/// <summary>
/// 已上传文件信息
/// </summary>
public class UploadedFileInfo
{
    /// <summary>
    /// 文件键
    /// </summary>
    public string FileKey { get; set; } = string.Empty;

    /// <summary>
    /// 下载加密查询参数
    /// </summary>
    public string DownloadEncryptedQueryParam { get; set; } = string.Empty;

    /// <summary>
    /// AES密钥（十六进制编码）
    /// </summary>
    public string AesKey { get; set; } = string.Empty;

    /// <summary>
    /// 明文文件大小（字节）
    /// </summary>
    public int FileSize { get; set; }

    /// <summary>
    /// 密文文件大小（字节）
    /// </summary>
    public int FileSizeCiphertext { get; set; }
}

/// <summary>
/// 媒体上传服务
/// 处理文件上传到微信CDN
/// </summary>
public class MediaUploadService
{
    private readonly WeixinApiClient _apiClient;
    private readonly Cdn.CdnClient _cdnClient;
    private readonly string _cdnBaseUrl;
    private readonly ILogger? _logger;

    /// <summary>
    /// 创建MediaUploadService实例
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="logger">日志记录器</param>
    public MediaUploadService(WeixinApiClient apiClient, string cdnBaseUrl, ILogger? logger = null)
    {
        _apiClient = apiClient;
        _cdnBaseUrl = cdnBaseUrl;
        _logger = logger;
        _cdnClient = new CdnClient(logger);
    }

    /// <summary>
    /// 上传文件到微信CDN
    /// </summary>
    /// <param name="filePath">本地文件路径</param>
    /// <param name="toUserId">目标用户ID</param>
    /// <param name="mediaType">媒体类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传文件信息</returns>
    public async Task<UploadedFileInfo> UploadFileAsync(
        string filePath,
        string toUserId,
        Models.UploadMediaType mediaType,
        CancellationToken cancellationToken = default)
    {
        var plaintext = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var rawSize = plaintext.Length;
        var rawFileMd5 = Convert.ToHexString(MD5.HashData(plaintext)).ToLowerInvariant();
        var fileSize = Cdn.AesEcbCrypto.GetPaddedSize(rawSize);
        var fileKey = GenerateFileKey();
        var aesKey = Cdn.AesEcbCrypto.GenerateKey();

        _logger?.LogDebug("UploadFile: file={FilePath} rawSize={RawSize} fileSize={FileSize} md5={Md5} fileKey={FileKey}",
            filePath, rawSize, fileSize, rawFileMd5, fileKey);

        var uploadUrlResp = await _apiClient.GetUploadUrlAsync(new Models.GetUploadUrlRequest
        {
            FileKey = fileKey,
            MediaType = (int)mediaType,
            ToUserId = toUserId,
            RawSize = rawSize,
            RawFileMd5 = rawFileMd5,
            FileSize = fileSize,
            NoNeedThumb = true,
            AesKey = Convert.ToHexString(aesKey).ToLowerInvariant()
        }, cancellationToken);

        string downloadParam;

        if (!string.IsNullOrEmpty(uploadUrlResp.UploadFullUrl))
        {
            downloadParam = await _cdnClient.UploadBufferByUrlAsync(
                plaintext,
                uploadUrlResp.UploadFullUrl,
                aesKey,
                $"UploadFile[fileKey={fileKey}]",
                cancellationToken);
        }
        else
        {
            var uploadParam = uploadUrlResp.UploadParam;
            if (string.IsNullOrEmpty(uploadParam))
            {
                _logger?.LogError("UploadFile: getUploadUrl returned no upload_param and no upload_full_url");
                throw new InvalidOperationException("getUploadUrl returned no upload_param and no upload_full_url");
            }

            downloadParam = await _cdnClient.UploadBufferAsync(
                plaintext,
                uploadParam,
                fileKey,
                _cdnBaseUrl,
                aesKey,
                $"UploadFile[fileKey={fileKey}]",
                cancellationToken);
        }

        return new UploadedFileInfo
        {
            FileKey = fileKey,
            DownloadEncryptedQueryParam = downloadParam,
            AesKey = Convert.ToHexString(aesKey).ToLowerInvariant(),
            FileSize = rawSize,
            FileSizeCiphertext = fileSize
        };
    }

    /// <summary>
    /// 上传图片
    /// </summary>
    public Task<UploadedFileInfo> UploadImageAsync(string filePath, string toUserId, CancellationToken cancellationToken = default)
    {
        return UploadFileAsync(filePath, toUserId, Models.UploadMediaType.Image, cancellationToken);
    }

    /// <summary>
    /// 上传视频
    /// </summary>
    public Task<UploadedFileInfo> UploadVideoAsync(string filePath, string toUserId, CancellationToken cancellationToken = default)
    {
        return UploadFileAsync(filePath, toUserId, Models.UploadMediaType.Video, cancellationToken);
    }

    /// <summary>
    /// 上传文件附件
    /// </summary>
    public Task<UploadedFileInfo> UploadFileAttachmentAsync(string filePath, string toUserId, CancellationToken cancellationToken = default)
    {
        return UploadFileAsync(filePath, toUserId, Models.UploadMediaType.File, cancellationToken);
    }

    /// <summary>
    /// 生成文件键
    /// </summary>
    private static string GenerateFileKey()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
