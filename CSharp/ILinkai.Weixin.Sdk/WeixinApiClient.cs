using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk;

/// <summary>
/// ILinkai微信API客户端配置选项
/// </summary>
public class WeixinApiClientOptions
{
    /// <summary>
    /// API基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://ilinkai.weixin.qq.com";

    /// <summary>
    /// CDN基础URL
    /// </summary>
    public string CdnBaseUrl { get; set; } = "https://novac2c.cdn.weixin.qq.com/c2c";

    /// <summary>
    /// 认证令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 请求超时时间（毫秒），默认15000
    /// </summary>
    public int TimeoutMs { get; set; } = 15000;

    /// <summary>
    /// 长轮询超时时间（毫秒），默认35000
    /// </summary>
    public int LongPollTimeoutMs { get; set; } = 35000;

    /// <summary>
    /// 配置请求超时时间（毫秒），默认10000
    /// </summary>
    public int ConfigTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// 路由标签
    /// </summary>
    public string? RouteTag { get; set; }
}

/// <summary>
/// ILinkai微信API客户端
/// 提供与ilinkai.weixin.qq.com API交互的核心功能
/// </summary>
public class WeixinApiClient
{
    private readonly WeixinApiClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly string _channelVersion;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 默认API基础URL
    /// </summary>
    public const string DefaultBaseUrl = "https://ilinkai.weixin.qq.com";

    /// <summary>
    /// 默认CDN基础URL
    /// </summary>
    public const string DefaultCdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";

    /// <summary>
    /// 会话过期错误码
    /// </summary>
    public const int SessionExpiredErrorCode = -14;

    /// <summary>
    /// 创建WeixinApiClient实例
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志记录器</param>
    public WeixinApiClient(WeixinApiClientOptions? options = null, ILogger? logger = null)
    {
        _options = options ?? new WeixinApiClientOptions();
        _logger = logger;
        _channelVersion = GetChannelVersion();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(Math.Max(_options.TimeoutMs, _options.LongPollTimeoutMs) + 5000)
        };
    }

    /// <summary>
    /// 获取渠道版本号
    /// </summary>
    private static string GetChannelVersion()
    {
        try
        {
            var version = typeof(WeixinApiClient).Assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// 构建基础信息
    /// </summary>
    private Models.BaseInfo BuildBaseInfo()
    {
        return new Models.BaseInfo
        {
            ChannelVersion = _channelVersion
        };
    }

    /// <summary>
    /// 生成随机微信UIN头部值
    /// </summary>
    private static string GenerateRandomWechatUin()
    {
        var randomBytes = new byte[4];
        RandomNumberGenerator.Fill(randomBytes);
        var uint32Value = BitConverter.ToUInt32(randomBytes, 0);
        var decimalString = uint32Value.ToString();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(decimalString));
    }

    /// <summary>
    /// 构建请求头
    /// </summary>
    private Dictionary<string, string> BuildHeaders(string body)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["AuthorizationType"] = "ilink_bot_token",
            ["Content-Length"] = Encoding.UTF8.GetByteCount(body).ToString(),
            ["X-WECHAT-UIN"] = GenerateRandomWechatUin()
        };

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            headers["Authorization"] = $"Bearer {_options.Token.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(_options.RouteTag))
        {
            headers["SKRouteTag"] = _options.RouteTag;
        }

        return headers;
    }

    /// <summary>
    /// 确保URL以斜杠结尾
    /// </summary>
    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith("/") ? url : $"{url}/";
    }

    /// <summary>
    /// 发送API请求
    /// </summary>
    private async Task<string> ApiFetchAsync(string endpoint, object body, int timeoutMs, string label)
    {
        var baseUrl = EnsureTrailingSlash(_options.BaseUrl);
        var url = $"{baseUrl}{endpoint}";
        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
        var headers = BuildHeaders(jsonBody);

        _logger?.LogDebug("POST {Url} body={Body}", RedactUrl(url), RedactBody(jsonBody));

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        foreach (var header in headers)
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var cts = new CancellationTokenSource(timeoutMs);
        var response = await _httpClient.SendAsync(request, cts.Token);
        var rawText = await response.Content.ReadAsStringAsync(cts.Token);

        _logger?.LogDebug("{Label} status={Status} raw={Raw}", label, (int)response.StatusCode, RedactBody(rawText));

        if (!response.IsSuccessStatusCode)
        {
            throw new WeixinApiException($"{label} {(int)response.StatusCode}: {rawText}", (int)response.StatusCode);
        }

        return rawText;
    }

    /// <summary>
    /// 获取更新（长轮询）
    /// </summary>
    /// <param name="getUpdatesBuf">更新缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新响应</returns>
    public async Task<Models.GetUpdatesResponse> GetUpdatesAsync(string? getUpdatesBuf = null, CancellationToken cancellationToken = default)
    {
        var request = new Models.GetUpdatesRequest
        {
            GetUpdatesBuf = getUpdatesBuf ?? string.Empty,
            BaseInfo = BuildBaseInfo()
        };

        try
        {
            var rawText = await ApiFetchAsync("ilink/bot/getupdates", request, _options.LongPollTimeoutMs, "getUpdates");
            return JsonSerializer.Deserialize<Models.GetUpdatesResponse>(rawText, JsonOptions)
                ?? new Models.GetUpdatesResponse();
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("getUpdates: client-side timeout after {Timeout}ms, returning empty response", _options.LongPollTimeoutMs);
            return new Models.GetUpdatesResponse
            {
                Ret = 0,
                Messages = new List<Models.WeixinMessage>(),
                GetUpdatesBuf = getUpdatesBuf
            };
        }
    }

    /// <summary>
    /// 获取上传URL
    /// </summary>
    /// <param name="request">请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传URL响应</returns>
    public async Task<Models.GetUploadUrlResponse> GetUploadUrlAsync(Models.GetUploadUrlRequest request, CancellationToken cancellationToken = default)
    {
        request.BaseInfo = BuildBaseInfo();
        var rawText = await ApiFetchAsync("ilink/bot/getuploadurl", request, _options.TimeoutMs, "getUploadUrl");
        return JsonSerializer.Deserialize<Models.GetUploadUrlResponse>(rawText, JsonOptions)
            ?? new Models.GetUploadUrlResponse();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="request">发送消息请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendMessageAsync(Models.SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Message != null)
        {
            request.Message = new Models.WeixinMessage
            {
                Seq = request.Message.Seq,
                MessageId = request.Message.MessageId,
                FromUserId = request.Message.FromUserId,
                ToUserId = request.Message.ToUserId,
                ClientId = request.Message.ClientId,
                CreateTimeMs = request.Message.CreateTimeMs,
                UpdateTimeMs = request.Message.UpdateTimeMs,
                DeleteTimeMs = request.Message.DeleteTimeMs,
                SessionId = request.Message.SessionId,
                GroupId = request.Message.GroupId,
                MessageType = request.Message.MessageType,
                MessageState = request.Message.MessageState,
                ItemList = request.Message.ItemList,
                ContextToken = request.Message.ContextToken
            };
        }
        await ApiFetchAsync("ilink/bot/sendmessage", request, _options.TimeoutMs, "sendMessage");
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    /// <param name="ilinkUserId">ILink用户ID</param>
    /// <param name="contextToken">上下文令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置响应</returns>
    public async Task<Models.GetConfigResponse> GetConfigAsync(string ilinkUserId, string? contextToken = null, CancellationToken cancellationToken = default)
    {
        var request = new Models.GetConfigRequest
        {
            ILinkUserId = ilinkUserId,
            ContextToken = contextToken,
            BaseInfo = BuildBaseInfo()
        };
        var rawText = await ApiFetchAsync("ilink/bot/getconfig", request, _options.ConfigTimeoutMs, "getConfig");
        return JsonSerializer.Deserialize<Models.GetConfigResponse>(rawText, JsonOptions)
            ?? new Models.GetConfigResponse();
    }

    /// <summary>
    /// 发送输入状态
    /// </summary>
    /// <param name="request">输入状态请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendTypingAsync(Models.SendTypingRequest request, CancellationToken cancellationToken = default)
    {
        request.BaseInfo = BuildBaseInfo();
        await ApiFetchAsync("ilink/bot/sendtyping", request, _options.ConfigTimeoutMs, "sendTyping");
    }

    /// <summary>
    /// 脱敏URL
    /// </summary>
    private static string RedactUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = uri.Query;
            if (string.IsNullOrEmpty(query)) return url;
            return url.Split('?')[0] + "?[REDACTED]";
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// 脱敏请求体
    /// </summary>
    private static string RedactBody(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length < 200) return body;
        return body.Substring(0, 200) + "...[TRUNCATED]";
    }
}

/// <summary>
/// 微信API异常
/// </summary>
public class WeixinApiException : Exception
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 创建WeixinApiException实例
    /// </summary>
    public WeixinApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
