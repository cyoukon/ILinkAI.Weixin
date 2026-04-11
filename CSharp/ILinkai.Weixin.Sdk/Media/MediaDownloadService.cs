using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk.Cdn;

namespace ILinkai.Weixin.Sdk.Media;

/// <summary>
/// 媒体下载服务
/// 处理从微信CDN下载和解密媒体文件
/// </summary>
public class MediaDownloadService
{
    private readonly Cdn.CdnClient _cdnClient;
    private readonly string _cdnBaseUrl;
    private readonly ILogger? _logger;

    /// <summary>
    /// 最大媒体文件大小（字节）
    /// </summary>
    public const int MaxMediaBytes = 100 * 1024 * 1024;

    /// <summary>
    /// 创建MediaDownloadService实例
    /// </summary>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="logger">日志记录器</param>
    public MediaDownloadService(string cdnBaseUrl, ILogger? logger = null)
    {
        _cdnBaseUrl = cdnBaseUrl;
        _logger = logger;
        _cdnClient = new CdnClient(logger);
    }

    /// <summary>
    /// 下载并解密图片
    /// </summary>
    /// <param name="encryptQueryParam">加密查询参数</param>
    /// <param name="aesKeyBase64">Base64编码的AES密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的图片数据</returns>
    public async Task<byte[]> DownloadImageAsync(
        string encryptQueryParam,
        string? aesKeyBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptQueryParam))
        {
            throw new ArgumentException("encryptQueryParam is required", nameof(encryptQueryParam));
        }

        _logger?.LogDebug("DownloadImage: encrypt_query_param={Param} hasAesKey={HasKey}",
            encryptQueryParam.Substring(0, Math.Min(40, encryptQueryParam.Length)),
            !string.IsNullOrEmpty(aesKeyBase64));

        try
        {
            if (!string.IsNullOrEmpty(aesKeyBase64))
            {
                return await _cdnClient.DownloadAndDecryptAsync(
                    encryptQueryParam,
                    aesKeyBase64,
                    _cdnBaseUrl,
                    "DownloadImage",
                    cancellationToken);
            }
            else
            {
                return await _cdnClient.DownloadPlainAsync(
                    encryptQueryParam,
                    _cdnBaseUrl,
                    "DownloadImage-plain",
                    cancellationToken);
            }
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "DownloadImage: download/decrypt failed");
            throw;
        }
    }

    /// <summary>
    /// 下载并解密语音
    /// </summary>
    /// <param name="encryptQueryParam">加密查询参数</param>
    /// <param name="aesKeyBase64">Base64编码的AES密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的语音数据</returns>
    public async Task<byte[]> DownloadVoiceAsync(
        string encryptQueryParam,
        string aesKeyBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptQueryParam) || string.IsNullOrEmpty(aesKeyBase64))
        {
            throw new ArgumentException("encryptQueryParam and aesKeyBase64 are required");
        }

        _logger?.LogDebug("DownloadVoice: downloading...");

        try
        {
            var silkData = await _cdnClient.DownloadAndDecryptAsync(
                encryptQueryParam,
                aesKeyBase64,
                _cdnBaseUrl,
                "DownloadVoice",
                cancellationToken);

            _logger?.LogDebug("DownloadVoice: decrypted {Size} bytes", silkData.Length);

            return silkData;
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "DownloadVoice: download/decrypt failed");
            throw;
        }
    }

    /// <summary>
    /// 下载并解密文件
    /// </summary>
    /// <param name="encryptQueryParam">加密查询参数</param>
    /// <param name="aesKeyBase64">Base64编码的AES密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的文件数据</returns>
    public async Task<byte[]> DownloadFileAsync(
        string encryptQueryParam,
        string aesKeyBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptQueryParam) || string.IsNullOrEmpty(aesKeyBase64))
        {
            throw new ArgumentException("encryptQueryParam and aesKeyBase64 are required");
        }

        _logger?.LogDebug("DownloadFile: downloading...");

        try
        {
            var data = await _cdnClient.DownloadAndDecryptAsync(
                encryptQueryParam,
                aesKeyBase64,
                _cdnBaseUrl,
                "DownloadFile",
                cancellationToken);

            _logger?.LogDebug("DownloadFile: downloaded {Size} bytes", data.Length);

            return data;
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "DownloadFile: download failed");
            throw;
        }
    }

    /// <summary>
    /// 下载并解密视频
    /// </summary>
    /// <param name="encryptQueryParam">加密查询参数</param>
    /// <param name="aesKeyBase64">Base64编码的AES密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的视频数据</returns>
    public async Task<byte[]> DownloadVideoAsync(
        string encryptQueryParam,
        string aesKeyBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptQueryParam) || string.IsNullOrEmpty(aesKeyBase64))
        {
            throw new ArgumentException("encryptQueryParam and aesKeyBase64 are required");
        }

        _logger?.LogDebug("DownloadVideo: downloading...");

        try
        {
            var data = await _cdnClient.DownloadAndDecryptAsync(
                encryptQueryParam,
                aesKeyBase64,
                _cdnBaseUrl,
                "DownloadVideo",
                cancellationToken);

            _logger?.LogDebug("DownloadVideo: downloaded {Size} bytes", data.Length);

            return data;
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "DownloadVideo: download failed");
            throw;
        }
    }

    /// <summary>
    /// 保存媒体数据到文件
    /// </summary>
    /// <param name="data">媒体数据</param>
    /// <param name="directory">目标目录</param>
    /// <param name="fileName">文件名（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的文件路径</returns>
    public async Task<string> SaveMediaToFileAsync(
        byte[] data,
        string directory,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directory);

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"media_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.bin";
        }

        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);

        _logger?.LogDebug("SaveMediaToFile: saved to {FilePath}", filePath);

        return filePath;
    }
}
