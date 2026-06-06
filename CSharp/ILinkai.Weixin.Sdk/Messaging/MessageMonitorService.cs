using System.Text.Json;
using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk.Auth;

namespace ILinkai.Weixin.Sdk.Messaging;

/// <summary>
/// 消息监控服务选项
/// </summary>
public class MessageMonitorOptions
{
    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = WeixinApiClient.DefaultBaseUrl;

    /// <summary>
    /// CDN基础URL
    /// </summary>
    public string CdnBaseUrl { get; set; } = WeixinApiClient.DefaultCdnBaseUrl;

    /// <summary>
    /// 认证令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// 长轮询超时时间（毫秒）
    /// </summary>
    public int LongPollTimeoutMs { get; set; } = 35_000;

    /// <summary>
    /// 媒体存储目录
    /// </summary>
    public string? MediaDir { get; set; }
}

/// <summary>
/// 消息监控服务
/// 长轮询获取微信消息
/// </summary>
public class MessageMonitorService : IDisposable
{
    private readonly MessageMonitorOptions _options;
    private readonly WeixinApiClient _apiClient;
    private readonly Auth.SessionGuard _sessionGuard;
    private readonly MessageProcessor _messageProcessor;
    private readonly ILogger? _logger;
    private readonly string _syncBufFilePath;

    private const int MaxConsecutiveFailures = 3;
    private const int BackoffDelayMs = 30_000;
    private const int RetryDelayMs = 2_000;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    private readonly StatusChangedEventArgs _status = new();

    /// <summary>
    /// 接收到消息时触发
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// 发生错误时触发
    /// </summary>
    public event EventHandler<ErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// 状态变化时触发
    /// </summary>
    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// 创建MessageMonitorService实例
    /// </summary>
    /// <param name="options">监控选项</param>
    /// <param name="logger">日志记录器</param>
    public MessageMonitorService(MessageMonitorOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        _apiClient = new WeixinApiClient(new WeixinApiClientOptions
        {
            BaseUrl = options.BaseUrl,
            CdnBaseUrl = options.CdnBaseUrl,
            Token = options.Token,
            LongPollTimeoutMs = options.LongPollTimeoutMs
        }, logger);
        _sessionGuard = new Auth.SessionGuard();
        _messageProcessor = new MessageProcessor(options.CdnBaseUrl, logger);
        _status.AccountId = options.AccountId;

        var stateDir = GetStateDir();
        Directory.CreateDirectory(stateDir);
        _syncBufFilePath = Path.Combine(stateDir, $"{Auth.AccountStore.NormalizeAccountId(options.AccountId)}_sync_buf.json");
    }

    /// <summary>
    /// 获取状态目录
    /// </summary>
    private static string GetStateDir()
    {
        var envPath = Environment.GetEnvironmentVariable("ILINKAI_STATE_DIR");
        if (!string.IsNullOrWhiteSpace(envPath)) return envPath;

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".ilinkai", "sync");
    }

    /// <summary>
    /// 加载同步缓冲区
    /// </summary>
    private string LoadSyncBuf()
    {
        try
        {
            if (!File.Exists(_syncBufFilePath)) return string.Empty;
            var raw = File.ReadAllText(_syncBufFilePath);
            var parsed = JsonSerializer.Deserialize<SyncBufData>(raw);
            return parsed?.GetUpdatesBuf ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 保存同步缓冲区
    /// </summary>
    private void SaveSyncBuf(string syncBuf)
    {
        try
        {
            var data = new SyncBufData { GetUpdatesBuf = syncBuf };
            File.WriteAllText(_syncBufFilePath, JsonSerializer.Serialize(data));
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "Failed to save sync buf");
        }
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        _logger?.LogInformation("Monitor started: baseUrl={BaseUrl} accountId={AccountId}",
            _options.BaseUrl, _options.AccountId);

        _status.Running = true;
        _status.LastStartAt = DateTime.UtcNow;
        StatusChanged?.Invoke(this, _status);

        var syncBuf = LoadSyncBuf();
        var nextTimeoutMs = _options.LongPollTimeoutMs;
        var consecutiveFailures = 0;

        while (!token.IsCancellationRequested)
        {
            try
            {
                _sessionGuard.AssertSessionActive(_options.AccountId);

                var response = await _apiClient.GetUpdatesAsync(syncBuf, token);

                _logger?.LogDebug("getUpdates response: ret={Ret} msgs={Count} bufLen={Length}",
                    response.Ret, response.Messages?.Count ?? 0, response.GetUpdatesBuf?.Length ?? 0);

                if (response.LongPollingTimeoutMs.HasValue && response.LongPollingTimeoutMs > 0)
                {
                    nextTimeoutMs = response.LongPollingTimeoutMs.Value;
                }

                var isApiError = (response.Ret.HasValue && response.Ret != 0) ||
                                 (response.ErrCode.HasValue && response.ErrCode != 0);

                if (isApiError)
                {
                    var isSessionExpired = response.ErrCode == Auth.SessionGuard.SessionExpiredErrorCode ||
                                           response.Ret == Auth.SessionGuard.SessionExpiredErrorCode;

                    if (isSessionExpired)
                    {
                        _sessionGuard.PauseSession(_options.AccountId);
                        var pauseMs = _sessionGuard.GetRemainingPauseMs(_options.AccountId);

                        _logger?.LogError("getUpdates: session expired, pausing for {Minutes} min",
                            Math.Ceiling(pauseMs / 60000.0));

                        ErrorOccurred?.Invoke(this, new ErrorEventArgs
                        {
                            AccountId = _options.AccountId,
                            Error = new WeixinSessionPausedException(_options.AccountId, (int)Math.Ceiling(pauseMs / 60000.0))
                        });

                        consecutiveFailures = 0;
                        await Task.Delay((int)pauseMs, token);
                        continue;
                    }

                    consecutiveFailures++;
                    _logger?.LogError("getUpdates failed: ret={Ret} errcode={ErrCode} errmsg={ErrMsg} ({Count}/{Max})",
                        response.Ret, response.ErrCode, response.ErrMsg, consecutiveFailures, MaxConsecutiveFailures);

                    if (consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        _logger?.LogError("getUpdates: {Max} consecutive failures, backing off 30s", MaxConsecutiveFailures);
                        consecutiveFailures = 0;
                        await Task.Delay(BackoffDelayMs, token);
                    }
                    else
                    {
                        await Task.Delay(RetryDelayMs, token);
                    }
                    continue;
                }

                consecutiveFailures = 0;
                _status.LastEventAt = DateTime.UtcNow;
                StatusChanged?.Invoke(this, _status);

                if (!string.IsNullOrEmpty(response.GetUpdatesBuf))
                {
                    SaveSyncBuf(response.GetUpdatesBuf);
                    syncBuf = response.GetUpdatesBuf;
                }

                var messages = response.Messages ?? new List<Models.WeixinMessage>();
                foreach (var msg in messages)
                {
                    _logger?.LogInformation("inbound message: from={From} types={Types}",
                        msg.FromUserId,
                        string.Join(",", msg.ItemList?.Select(i => i.Type) ?? Array.Empty<int?>()));

                    _status.LastEventAt = DateTime.UtcNow;
                    _status.LastInboundAt = DateTime.UtcNow;
                    StatusChanged?.Invoke(this, _status);

                    var context = _messageProcessor.ConvertToContext(msg, _options.AccountId);

                    if (!string.IsNullOrEmpty(_options.MediaDir))
                    {
                        await _messageProcessor.ProcessMediaAsync(context, _options.MediaDir, token);
                    }

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                    {
                        AccountId = _options.AccountId,
                        Message = msg,
                        Context = context
                    });
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger?.LogInformation("Monitor stopped (cancelled)");
                break;
            }
            catch (WeixinSessionPausedException)
            {
                throw;
            }
            catch (Exception err)
            {
                consecutiveFailures++;
                _logger?.LogError(err, "getUpdates error ({Count}/{Max})", consecutiveFailures, MaxConsecutiveFailures);

                ErrorOccurred?.Invoke(this, new ErrorEventArgs
                {
                    AccountId = _options.AccountId,
                    Error = err
                });

                if (consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _logger?.LogError("getUpdates: {Max} consecutive failures, backing off 30s", MaxConsecutiveFailures);
                    consecutiveFailures = 0;
                    await Task.Delay(BackoffDelayMs, token);
                }
                else
                {
                    await Task.Delay(RetryDelayMs, token);
                }
            }
        }

        _status.Running = false;
        StatusChanged?.Invoke(this, _status);

        _logger?.LogInformation("Monitor ended");
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _cancellationTokenSource?.Dispose();
    }

    private class SyncBufData
    {
        [System.Text.Json.Serialization.JsonPropertyName("get_updates_buf")]
        public string? GetUpdatesBuf { get; set; }
    }
}

/// <summary>
/// 消息接收事件参数
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// 原始消息
    /// </summary>
    public Models.WeixinMessage Message { get; set; } = null!;

    /// <summary>
    /// 消息上下文
    /// </summary>
    public MessageContext Context { get; set; } = null!;
}

/// <summary>
/// 错误事件参数
/// </summary>
public class ErrorEventArgs : EventArgs
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// 错误
    /// </summary>
    public Exception Error { get; set; } = null!;
}

/// <summary>
/// 状态变化事件参数
/// </summary>
public class StatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// 是否运行中
    /// </summary>
    public bool Running { get; set; }

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastStartAt { get; set; }

    /// <summary>
    /// 最后事件时间
    /// </summary>
    public DateTime? LastEventAt { get; set; }

    /// <summary>
    /// 最后接收消息时间
    /// </summary>
    public DateTime? LastInboundAt { get; set; }
}
