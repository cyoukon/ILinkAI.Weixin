using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Cdn;

/// <summary>
/// CDN客户端服务
/// 处理文件的上传和下载
/// </summary>
public class CdnClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private const int MaxRetries = 3;

    /// <summary>
    /// 创建CdnClient实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public CdnClient(ILogger? logger = null)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 上传缓冲区到CDN（带AES-128-ECB加密）
    /// </summary>
    /// <param name="buffer">要上传的数据</param>
    /// <param name="uploadParam">上传参数</param>
    /// <param name="fileKey">文件键</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="aesKey">AES密钥</param>
    /// <param name="label">日志标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载参数</returns>
    public async Task<string> UploadBufferAsync(
        byte[] buffer,
        string uploadParam,
        string fileKey,
        string cdnBaseUrl,
        byte[] aesKey,
        string label = "upload",
        CancellationToken cancellationToken = default)
    {
        var cdnUrl = CdnUrlBuilder.BuildUploadUrl(uploadParam, fileKey, cdnBaseUrl);
        return await UploadBufferCoreAsync(buffer, cdnUrl, aesKey, label, cancellationToken);
    }

    public async Task<string> UploadBufferByUrlAsync(
        byte[] buffer,
        string uploadFullUrl,
        byte[] aesKey,
        string label = "upload",
        CancellationToken cancellationToken = default)
    {
        return await UploadBufferCoreAsync(buffer, uploadFullUrl, aesKey, label, cancellationToken);
    }

    private async Task<string> UploadBufferCoreAsync(
        byte[] buffer,
        string cdnUrl,
        byte[] aesKey,
        string label,
        CancellationToken cancellationToken)
    {
        var ciphertext = AesEcbCrypto.Encrypt(buffer, aesKey);

        _logger?.LogDebug("{Label}: CDN POST url={Url} ciphertextSize={Size}", label, RedactUrl(cdnUrl), ciphertext.Length);

        Exception? lastError = null;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, cdnUrl);
                request.Content = new ByteArrayContent(ciphertext);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    var errMsg = response.Headers.TryGetValues("x-error-message", out var msgs)
                        ? string.Join(", ", msgs)
                        : await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger?.LogError("{Label}: CDN client error attempt={Attempt} status={Status} errMsg={ErrMsg}",
                        label, attempt, (int)response.StatusCode, errMsg);

                    throw new CdnUploadException($"CDN upload client error {(int)response.StatusCode}: {errMsg}", (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = response.Headers.TryGetValues("x-error-message", out var msgs)
                        ? string.Join(", ", msgs)
                        : $"status {(int)response.StatusCode}";

                    _logger?.LogError("{Label}: CDN server error attempt={Attempt} status={Status} errMsg={ErrMsg}",
                        label, attempt, (int)response.StatusCode, errMsg);

                    throw new CdnUploadException($"CDN upload server error: {errMsg}", (int)response.StatusCode);
                }

                var downloadParam = response.Headers.TryGetValues("x-encrypted-param", out var values)
                    ? values.FirstOrDefault()
                    : null;

                if (string.IsNullOrEmpty(downloadParam))
                {
                    _logger?.LogError("{Label}: CDN response missing x-encrypted-param header attempt={Attempt}",
                        label, attempt);
                    throw new CdnUploadException("CDN upload response missing x-encrypted-param header");
                }

                _logger?.LogDebug("{Label}: CDN upload success attempt={Attempt}", label, attempt);
                return downloadParam;
            }
            catch (CdnUploadException)
            {
                throw;
            }
            catch (Exception err)
            {
                lastError = err;
                if (attempt < MaxRetries)
                {
                    _logger?.LogError("{Label}: attempt {Attempt} failed, retrying... err={Error}", label, attempt, err.Message);
                }
                else
                {
                    _logger?.LogError("{Label}: all {MaxRetries} attempts failed err={Error}", label, MaxRetries, err.Message);
                }
            }
        }

        throw lastError ?? new CdnUploadException($"CDN upload failed after {MaxRetries} attempts");
    }

    /// <summary>
    /// 从CDN下载并解密数据
    /// </summary>
    /// <param name="encryptedQueryParam">加密查询参数</param>
    /// <param name="aesKeyBase64">Base64编码的AES密钥</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="label">日志标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的数据</returns>
    public async Task<byte[]> DownloadAndDecryptAsync(
        string encryptedQueryParam,
        string aesKeyBase64,
        string cdnBaseUrl,
        string label = "download",
        CancellationToken cancellationToken = default)
    {
        var key = ParseAesKey(aesKeyBase64, label);
        var url = CdnUrlBuilder.BuildDownloadUrl(encryptedQueryParam, cdnBaseUrl);

        _logger?.LogDebug("{Label}: fetching url={Url}", label, RedactUrl(url));

        var encrypted = await FetchCdnBytesAsync(url, label, cancellationToken);

        _logger?.LogDebug("{Label}: downloaded {Size} bytes, decrypting", label, encrypted.Length);

        var decrypted = AesEcbCrypto.Decrypt(encrypted, key);

        _logger?.LogDebug("{Label}: decrypted {Size} bytes", label, decrypted.Length);

        return decrypted;
    }

    /// <summary>
    /// 从CDN下载原始数据（不解密）
    /// </summary>
    /// <param name="encryptedQueryParam">加密查询参数</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="label">日志标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>原始数据</returns>
    public async Task<byte[]> DownloadPlainAsync(
        string encryptedQueryParam,
        string cdnBaseUrl,
        string label = "download",
        CancellationToken cancellationToken = default)
    {
        var url = CdnUrlBuilder.BuildDownloadUrl(encryptedQueryParam, cdnBaseUrl);
        _logger?.LogDebug("{Label}: fetching url={Url}", label, RedactUrl(url));
        return await FetchCdnBytesAsync(url, label, cancellationToken);
    }

    /// <summary>
    /// 从CDN获取原始字节
    /// </summary>
    private async Task<byte[]> FetchCdnBytesAsync(string url, string label, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception err)
        {
            _logger?.LogError("{Label}: fetch network error url={Url} err={Error}", label, RedactUrl(url), err.Message);
            throw;
        }

        _logger?.LogDebug("{Label}: response status={Status} ok={OK}", label, (int)response.StatusCode, response.IsSuccessStatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var msg = $"{label}: CDN download {(int)response.StatusCode} {response.ReasonPhrase} body={body}";
            _logger?.LogError("{Message}", msg);
            throw new CdnDownloadException(msg, (int)response.StatusCode);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// 解析AES密钥
    /// 支持两种格式：
    /// - Base64编码的原始16字节
    /// - Base64编码的32字符十六进制字符串
    /// </summary>
    private static byte[] ParseAesKey(string aesKeyBase64, string label)
    {
        var decoded = Convert.FromBase64String(aesKeyBase64);

        if (decoded.Length == 16)
        {
            return decoded;
        }

        if (decoded.Length == 32)
        {
            var hexString = Encoding.ASCII.GetString(decoded);
            if (IsHexString(hexString))
            {
                return Convert.FromHexString(hexString);
            }
        }

        var msg = $"{label}: aes_key must decode to 16 raw bytes or 32-char hex string, got {decoded.Length} bytes";
        throw new ArgumentException(msg);
    }

    /// <summary>
    /// 检查字符串是否为有效的十六进制字符串
    /// </summary>
    private static bool IsHexString(string s)
    {
        return s.Length == 32 && s.All(c => "0123456789abcdefABCDEF".Contains(c));
    }

    /// <summary>
    /// 脱敏URL
    /// </summary>
    private static string RedactUrl(string url)
    {
        try
        {
            var questionIndex = url.IndexOf('?');
            return questionIndex >= 0 ? url.Substring(0, questionIndex) + "?[REDACTED]" : url;
        }
        catch
        {
            return url;
        }
    }
}

/// <summary>
/// CDN上传异常
/// </summary>
public class CdnUploadException : Exception
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 创建CdnUploadException实例
    /// </summary>
    public CdnUploadException(string message, int statusCode = 0) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// CDN下载异常
/// </summary>
public class CdnDownloadException : Exception
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 创建CdnDownloadException实例
    /// </summary>
    public CdnDownloadException(string message, int statusCode = 0) : base(message)
    {
        StatusCode = statusCode;
    }
}
