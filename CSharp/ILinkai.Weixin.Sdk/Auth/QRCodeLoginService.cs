using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 二维码登录服务
/// 提供微信扫码登录功能
/// </summary>
public class QRCodeLoginService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly string _apiBaseUrl;
    private readonly string? _routeTag;

    private const int ActiveLoginTtlMs = 5 * 60 * 1000;
    private const int QrLongPollTimeoutMs = 35_000;
    private const int MaxQrRefreshCount = 3;

    /// <summary>
    /// 默认ILink机器人类型
    /// </summary>
    public const string DefaultIlinkBotType = "3";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 创建QRCodeLoginService实例
    /// </summary>
    /// <param name="apiBaseUrl">API基础URL</param>
    /// <param name="routeTag">路由标签</param>
    /// <param name="logger">日志记录器</param>
    public QRCodeLoginService(string apiBaseUrl, string? routeTag = null, ILogger? logger = null)
    {
        _apiBaseUrl = apiBaseUrl ?? WeixinApiClient.DefaultBaseUrl;
        _routeTag = routeTag;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(QrLongPollTimeoutMs + 5000)
        };
    }

    /// <summary>
    /// 获取二维码
    /// </summary>
    /// <param name="botType">机器人类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>二维码响应</returns>
    public async Task<Models.GetQRCodeResponse> FetchQRCodeAsync(string botType = DefaultIlinkBotType, CancellationToken cancellationToken = default)
    {
        var baseUrl = _apiBaseUrl.EndsWith("/") ? _apiBaseUrl : $"{_apiBaseUrl}/";
        var url = $"{baseUrl}ilink/bot/get_bot_qrcode?bot_type={Uri.EscapeDataString(botType)}";

        _logger?.LogInformation("Fetching QR code from: {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(_routeTag))
        {
            request.Headers.TryAddWithoutValidation("SKRouteTag", _routeTag);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogError("QR code fetch failed: {Status} {Reason} body={Body}", (int)response.StatusCode, response.ReasonPhrase, body);
            throw new WeixinApiException($"Failed to fetch QR code: {(int)response.StatusCode} {response.ReasonPhrase}", (int)response.StatusCode);
        }

        var rawText = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Models.GetQRCodeResponse>(rawText, JsonOptions)
            ?? new Models.GetQRCodeResponse();
    }

    /// <summary>
    /// 轮询二维码状态
    /// </summary>
    /// <param name="qrcode">二维码标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>二维码状态响应</returns>
    public async Task<Models.GetQRCodeStatusResponse> PollQRStatusAsync(string qrcode, CancellationToken cancellationToken = default)
    {
        var baseUrl = _apiBaseUrl.EndsWith("/") ? _apiBaseUrl : $"{_apiBaseUrl}/";
        var url = $"{baseUrl}ilink/bot/get_qrcode_status?qrcode={Uri.EscapeDataString(qrcode)}";

        _logger?.LogDebug("Long-poll QR status from: {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("iLink-App-ClientVersion", "1");

        if (!string.IsNullOrWhiteSpace(_routeTag))
        {
            request.Headers.TryAddWithoutValidation("SKRouteTag", _routeTag);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(QrLongPollTimeoutMs);

            var response = await _httpClient.SendAsync(request, cts.Token);
            var rawText = await response.Content.ReadAsStringAsync(cts.Token);

            _logger?.LogDebug("pollQRStatus: HTTP {Status}, body={Body}", (int)response.StatusCode, rawText.Substring(0, Math.Min(200, rawText.Length)));

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("QR status poll failed: {Status} {Reason} body={Body}", (int)response.StatusCode, response.ReasonPhrase, rawText);
                throw new WeixinApiException($"Failed to poll QR status: {(int)response.StatusCode} {response.ReasonPhrase}", (int)response.StatusCode);
            }

            return JsonSerializer.Deserialize<Models.GetQRCodeStatusResponse>(rawText, JsonOptions)
                ?? new Models.GetQRCodeStatusResponse();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogDebug("pollQRStatus: client-side timeout after {Timeout}ms, returning wait", QrLongPollTimeoutMs);
            return new Models.GetQRCodeStatusResponse { Status = "wait" };
        }
    }

    /// <summary>
    /// 开始二维码登录
    /// </summary>
    /// <param name="options">登录选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录开始结果</returns>
    public async Task<QRLoginStartResult> StartLoginAsync(QRLoginOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new QRLoginOptions();
        var sessionKey = options.AccountId ?? Guid.NewGuid().ToString();
        var botType = options.BotType ?? DefaultIlinkBotType;

        _logger?.LogInformation("Starting Weixin login with bot_type={BotType}", botType);

        try
        {
            var qrResponse = await FetchQRCodeAsync(botType, cancellationToken);

            _logger?.LogInformation("QR code received, qrcode={QRCode} imgContentLen={Length}",
                RedactToken(qrResponse.QRCode ?? ""),
                qrResponse.QRCodeImgContent?.Length ?? 0);

            return new QRLoginStartResult
            {
                SessionKey = sessionKey,
                QRCode = qrResponse.QRCode ?? string.Empty,
                QRCodeUrl = qrResponse.QRCodeImgContent ?? string.Empty,
                Message = "使用微信扫描以下二维码，以完成连接。"
            };
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "Failed to start Weixin login");
            return new QRLoginStartResult
            {
                SessionKey = sessionKey,
                Message = $"Failed to start login: {err.Message}"
            };
        }
    }

    /// <summary>
    /// 等待登录完成
    /// </summary>
    /// <param name="qrcode">二维码标识</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <param name="onStatusChanged">状态变化回调</param>
    /// <param name="onQRRefreshed">二维码刷新回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录等待结果</returns>
    public async Task<QRLoginWaitResult> WaitForLoginAsync(
        string qrcode,
        int timeoutMs = 480_000,
        Action<string>? onStatusChanged = null,
        Action<string>? onQRRefreshed = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var qrRefreshCount = 1;
        var currentQRCode = qrcode;
        var scannedPrinted = false;

        _logger?.LogInformation("Starting to poll QR code status...");

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var statusResponse = await PollQRStatusAsync(currentQRCode, cancellationToken);
                _logger?.LogDebug("pollQRStatus: status={Status} hasBotToken={HasBotToken} hasBotId={HasBotId}",
                    statusResponse.Status,
                    !string.IsNullOrEmpty(statusResponse.BotToken),
                    !string.IsNullOrEmpty(statusResponse.ILinkBotId));

                switch (statusResponse.Status?.ToLowerInvariant())
                {
                    case "wait":
                        onStatusChanged?.Invoke(".");
                        break;

                    case "scaned":
                        if (!scannedPrinted)
                        {
                            onStatusChanged?.Invoke("\n👀 已扫码，在微信继续操作...\n");
                            scannedPrinted = true;
                        }
                        break;

                    case "expired":
                        qrRefreshCount++;
                        if (qrRefreshCount > MaxQrRefreshCount)
                        {
                            _logger?.LogWarning("waitForWeixinLogin: QR expired {MaxCount} times, giving up", MaxQrRefreshCount);
                            return new QRLoginWaitResult
                            {
                                Connected = false,
                                Message = "登录超时：二维码多次过期，请重新开始登录流程。"
                            };
                        }

                        onStatusChanged?.Invoke($"\n⏳ 二维码已过期，正在刷新...({qrRefreshCount}/{MaxQrRefreshCount})\n");
                        _logger?.LogInformation("waitForWeixinLogin: QR expired, refreshing ({Count}/{MaxCount})", qrRefreshCount, MaxQrRefreshCount);

                        try
                        {
                            var newQRResponse = await FetchQRCodeAsync(cancellationToken: cancellationToken);
                            currentQRCode = newQRResponse.QRCode ?? string.Empty;
                            scannedPrinted = false;

                            _logger?.LogInformation("waitForWeixinLogin: new QR code obtained");
                            onQRRefreshed?.Invoke(newQRResponse.QRCodeImgContent ?? string.Empty);
                        }
                        catch (Exception refreshErr)
                        {
                            _logger?.LogError(refreshErr, "waitForWeixinLogin: failed to refresh QR code");
                            return new QRLoginWaitResult
                            {
                                Connected = false,
                                Message = $"刷新二维码失败: {refreshErr.Message}"
                            };
                        }
                        break;

                    case "confirmed":
                        if (string.IsNullOrEmpty(statusResponse.ILinkBotId))
                        {
                            _logger?.LogError("Login confirmed but ilink_bot_id missing from response");
                            return new QRLoginWaitResult
                            {
                                Connected = false,
                                Message = "登录失败：服务器未返回 ilink_bot_id。"
                            };
                        }

                        _logger?.LogInformation("✅ Login confirmed! ilink_bot_id={BotId} ilink_user_id={UserId}",
                            statusResponse.ILinkBotId,
                            RedactToken(statusResponse.ILinkUserId ?? ""));

                        return new QRLoginWaitResult
                        {
                            Connected = true,
                            BotToken = statusResponse.BotToken,
                            AccountId = statusResponse.ILinkBotId,
                            BaseUrl = statusResponse.BaseUrl,
                            UserId = statusResponse.ILinkUserId,
                            Message = "✅ 与微信连接成功！"
                        };
                }
            }
            catch (Exception err)
            {
                _logger?.LogError(err, "Error polling QR status");
                return new QRLoginWaitResult
                {
                    Connected = false,
                    Message = $"Login failed: {err.Message}"
                };
            }

            await Task.Delay(1000, cancellationToken);
        }

        _logger?.LogWarning("waitForWeixinLogin: timed out waiting for QR scan");
        return new QRLoginWaitResult
        {
            Connected = false,
            Message = "登录超时，请重试。"
        };
    }

    /// <summary>
    /// 脱敏令牌
    /// </summary>
    private static string RedactToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length <= 8) return "***";
        return $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}";
    }
}

/// <summary>
/// 二维码登录选项
/// </summary>
public class QRLoginOptions
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// 机器人类型
    /// </summary>
    public string? BotType { get; set; }

    /// <summary>
    /// 是否强制刷新
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// 是否详细输出
    /// </summary>
    public bool Verbose { get; set; }
}

/// <summary>
/// 二维码登录开始结果
/// </summary>
public class QRLoginStartResult
{
    /// <summary>
    /// 会话密钥
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// 二维码标识
    /// </summary>
    public string QRCode { get; set; } = string.Empty;

    /// <summary>
    /// 二维码URL
    /// </summary>
    public string? QRCodeUrl { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 二维码登录等待结果
/// </summary>
public class QRLoginWaitResult
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool Connected { get; set; }

    /// <summary>
    /// 机器人令牌
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>
    /// 账户ID
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// 基础URL
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
