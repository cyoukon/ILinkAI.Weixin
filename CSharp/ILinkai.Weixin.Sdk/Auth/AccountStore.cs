using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 微信账户数据
/// </summary>
public class WeixinAccountData
{
    /// <summary>
    /// 认证令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 保存时间
    /// </summary>
    public DateTime? SavedAt { get; set; }

    /// <summary>
    /// 基础URL
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 关联的微信用户ID
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// 已解析的微信账户信息
/// </summary>
public class ResolvedWeixinAccount
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

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
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否已配置（已获取令牌）
    /// </summary>
    public bool Configured { get; set; }

    /// <summary>
    /// 账户名称
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// 账户存储服务
/// 管理微信账户的持久化存储
/// </summary>
public class AccountStore : IAccountStore
{
    private readonly string _stateDir;
    private readonly ILogger? _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 创建AccountStore实例
    /// </summary>
    /// <param name="stateDir">状态目录路径</param>
    /// <param name="logger">日志记录器</param>
    public AccountStore(string? stateDir = null, ILogger? logger = null)
    {
        _stateDir = stateDir ?? GetDefaultStateDir();
        _logger = logger;
    }

    /// <summary>
    /// 获取默认状态目录
    /// </summary>
    private static string GetDefaultStateDir()
    {
        var envPath = Environment.GetEnvironmentVariable("ILINKAI_STATE_DIR");
        if (!string.IsNullOrWhiteSpace(envPath)) return envPath;

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".ilinkai");
    }

    /// <summary>
    /// 获取微信状态目录
    /// </summary>
    private string GetWeixinStateDir() => Path.Combine(_stateDir, "weixin");

    /// <summary>
    /// 获取账户索引文件路径
    /// </summary>
    private string GetAccountIndexPath() => Path.Combine(GetWeixinStateDir(), "accounts.json");

    /// <summary>
    /// 获取账户目录
    /// </summary>
    private string GetAccountsDir() => Path.Combine(GetWeixinStateDir(), "accounts");

    /// <summary>
    /// 获取账户文件路径
    /// </summary>
    private string GetAccountFilePath(string accountId) => Path.Combine(GetAccountsDir(), $"{SanitizeAccountId(accountId)}.json");

    /// <summary>
    /// 清理账户ID中的特殊字符
    /// </summary>
    private static string SanitizeAccountId(string accountId)
    {
        var safe = accountId.Trim().ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(c, '_');
        }
        return safe.Replace("..", "_");
    }

    /// <summary>
    /// 规范化账户ID
    /// 将原始ID转换为文件系统安全的格式
    /// </summary>
    public static string NormalizeAccountId(string rawId)
    {
        var trimmed = rawId.Trim().ToLowerInvariant();
        if (trimmed.EndsWith("@im.bot"))
        {
            return trimmed.Replace("@im.bot", "-im-bot");
        }
        if (trimmed.EndsWith("@im.wechat"))
        {
            return trimmed.Replace("@im.wechat", "-im-wechat");
        }
        return trimmed;
    }

    /// <summary>
    /// 从规范化ID推导原始ID
    /// </summary>
    public static string? DeriveRawAccountId(string normalizedId)
    {
        if (normalizedId.EndsWith("-im-bot"))
        {
            return $"{normalizedId.Substring(0, normalizedId.Length - 7)}@im.bot";
        }
        if (normalizedId.EndsWith("-im-wechat"))
        {
            return $"{normalizedId.Substring(0, normalizedId.Length - 10)}@im.wechat";
        }
        return null;
    }

    /// <summary>
    /// 列出所有已注册的账户ID
    /// </summary>
    public List<string> ListAccountIds()
    {
        var filePath = GetAccountIndexPath();
        try
        {
            if (!File.Exists(filePath)) return new List<string>();
            var raw = File.ReadAllText(filePath);
            var parsed = JsonSerializer.Deserialize<List<string>>(raw);
            return parsed?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to list account ids from {FilePath}", filePath);
            return new List<string>();
        }
    }

    /// <summary>
    /// 注册账户ID到索引
    /// </summary>
    public void RegisterAccountId(string accountId)
    {
        var dir = GetWeixinStateDir();
        Directory.CreateDirectory(dir);

        var existing = ListAccountIds();
        if (existing.Contains(accountId)) return;

        existing.Add(accountId);
        File.WriteAllText(GetAccountIndexPath(), JsonSerializer.Serialize(existing, JsonOptions));
    }

    /// <summary>
    /// 加载账户数据
    /// </summary>
    public WeixinAccountData? LoadAccount(string accountId)
    {
        var filePath = GetAccountFilePath(accountId);
        if (File.Exists(filePath))
        {
            return ReadAccountFile(filePath);
        }

        var rawId = DeriveRawAccountId(accountId);
        if (rawId != null)
        {
            var compatPath = GetAccountFilePath(rawId);
            if (File.Exists(compatPath))
            {
                return ReadAccountFile(compatPath);
            }
        }

        return null;
    }

    /// <summary>
    /// 读取账户文件
    /// </summary>
    private WeixinAccountData? ReadAccountFile(string filePath)
    {
        try
        {
            var raw = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WeixinAccountData>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read account file {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// 保存账户数据
    /// </summary>
    public void SaveAccount(string accountId, WeixinAccountData data)
    {
        var dir = GetAccountsDir();
        Directory.CreateDirectory(dir);

        var existing = LoadAccount(accountId) ?? new WeixinAccountData();

        var merged = new WeixinAccountData
        {
            Token = data.Token?.Trim() ?? existing.Token,
            BaseUrl = data.BaseUrl?.Trim() ?? existing.BaseUrl,
            UserId = data.UserId ?? existing.UserId,
            SavedAt = DateTime.UtcNow
        };

        var filePath = GetAccountFilePath(accountId);
        File.WriteAllText(filePath, JsonSerializer.Serialize(merged, JsonOptions));

        try
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 清除账户数据
    /// </summary>
    public void ClearAccount(string accountId)
    {
        try
        {
            var filePath = GetAccountFilePath(accountId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var rawId = DeriveRawAccountId(accountId);
            if (rawId != null)
            {
                var compatPath = GetAccountFilePath(rawId);
                if (File.Exists(compatPath))
                {
                    File.Delete(compatPath);
                }
            }

            var existing = ListAccountIds();
            if (existing.Remove(accountId))
            {
                File.WriteAllText(GetAccountIndexPath(), JsonSerializer.Serialize(existing, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to clear account {AccountId}", accountId);
        }
    }

    /// <summary>
    /// 解析账户信息
    /// </summary>
    public ResolvedWeixinAccount ResolveAccount(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("accountId is required", nameof(accountId));
        }

        var id = NormalizeAccountId(accountId);
        var accountData = LoadAccount(id);

        return new ResolvedWeixinAccount
        {
            AccountId = id,
            BaseUrl = accountData?.BaseUrl?.Trim() ?? WeixinApiClient.DefaultBaseUrl,
            CdnBaseUrl = WeixinApiClient.DefaultCdnBaseUrl,
            Token = accountData?.Token?.Trim(),
            Enabled = true,
            Configured = !string.IsNullOrWhiteSpace(accountData?.Token)
        };
    }
}
