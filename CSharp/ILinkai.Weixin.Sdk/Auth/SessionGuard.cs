namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 会话守卫服务
/// 管理会话状态和暂停逻辑
/// </summary>
public class SessionGuard
{
    private const int SessionPauseDurationMs = 60 * 60 * 1000;
    private readonly Dictionary<string, long> _pauseUntilMap = new();

    /// <summary>
    /// 会话过期错误码
    /// </summary>
    public const int SessionExpiredErrorCode = -14;

    /// <summary>
    /// 暂停指定账户的所有API调用（持续一小时）
    /// </summary>
    /// <param name="accountId">账户ID</param>
    public void PauseSession(string accountId)
    {
        var until = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + SessionPauseDurationMs;
        _pauseUntilMap[accountId] = until;
    }

    /// <summary>
    /// 检查会话是否已暂停
    /// </summary>
    /// <param name="accountId">账户ID</param>
    /// <returns>是否已暂停</returns>
    public bool IsSessionPaused(string accountId)
    {
        if (!_pauseUntilMap.TryGetValue(accountId, out var until))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= until)
        {
            _pauseUntilMap.Remove(accountId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取剩余暂停时间（毫秒）
    /// </summary>
    /// <param name="accountId">账户ID</param>
    /// <returns>剩余毫秒数，未暂停时返回0</returns>
    public long GetRemainingPauseMs(string accountId)
    {
        if (!_pauseUntilMap.TryGetValue(accountId, out var until))
        {
            return 0;
        }

        var remaining = until - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (remaining <= 0)
        {
            _pauseUntilMap.Remove(accountId);
            return 0;
        }

        return remaining;
    }

    /// <summary>
    /// 断言会话处于活动状态
    /// 如果会话已暂停则抛出异常
    /// </summary>
    /// <param name="accountId">账户ID</param>
    /// <exception cref="WeixinSessionPausedException">会话已暂停时抛出</exception>
    public void AssertSessionActive(string accountId)
    {
        if (IsSessionPaused(accountId))
        {
            var remainingMin = Math.Ceiling(GetRemainingPauseMs(accountId) / 60000.0);
            throw new WeixinSessionPausedException(accountId, (int)remainingMin);
        }
    }

    /// <summary>
    /// 重置内部状态（仅用于测试）
    /// </summary>
    public void ResetForTest()
    {
        _pauseUntilMap.Clear();
    }
}

/// <summary>
/// 微信会话暂停异常
/// </summary>
public class WeixinSessionPausedException : Exception
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// 剩余暂停时间（分钟）
    /// </summary>
    public int RemainingMinutes { get; }

    /// <summary>
    /// 创建WeixinSessionPausedException实例
    /// </summary>
    public WeixinSessionPausedException(string accountId, int remainingMinutes)
        : base($"Session paused for accountId={accountId}, {remainingMinutes} min remaining (errcode {SessionGuard.SessionExpiredErrorCode})")
    {
        AccountId = accountId;
        RemainingMinutes = remainingMinutes;
    }
}
