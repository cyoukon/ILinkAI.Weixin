using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.SqlServer.Models;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.SqlServer;

/// <summary>
/// SQL Server账户存储实现
/// </summary>
public class SqlServerAccountStore : IAccountStore
{
    private readonly AccountDbContext _dbContext;
    private readonly ILogger? _logger;

    /// <summary>
    /// 创建SqlServerAccountStore实例
    /// </summary>
    public SqlServerAccountStore(AccountDbContext dbContext, ILogger? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 列出所有已注册的账户ID
    /// </summary>
    public List<string> ListAccountIds()
    {
        return _dbContext.Accounts
            .Select(a => a.AccountId)
            .ToList();
    }

    /// <summary>
    /// 注册账户ID到索引
    /// SQL Server实现中，账户在SaveAccount时自动创建
    /// </summary>
    public void RegisterAccountId(string accountId)
    {
        // SQL Server 实现中，账户在 SaveAccount 时自动创建
        // 此方法保留以保持接口兼容性
    }

    /// <summary>
    /// 加载账户数据
    /// </summary>
    public WeixinAccountData? LoadAccount(string accountId)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity == null) return null;

        return new WeixinAccountData
        {
            Token = entity.Token,
            SavedAt = entity.SavedAt,
            BaseUrl = entity.BaseUrl,
            UserId = entity.UserId
        };
    }

    /// <summary>
    /// 保存账户数据
    /// </summary>
    public void SaveAccount(string accountId, WeixinAccountData data)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity == null)
        {
            entity = new AccountEntity
            {
                AccountId = normalizedId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Accounts.Add(entity);
        }

        entity.Token = data.Token?.Trim() ?? entity.Token;
        entity.BaseUrl = data.BaseUrl?.Trim() ?? entity.BaseUrl;
        entity.UserId = data.UserId ?? entity.UserId;
        entity.SavedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();
    }

    /// <summary>
    /// 清除账户数据
    /// </summary>
    public void ClearAccount(string accountId)
    {
        var normalizedId = AccountStore.NormalizeAccountId(accountId);
        var entity = _dbContext.Accounts.Find(normalizedId);

        if (entity != null)
        {
            _dbContext.Accounts.Remove(entity);
            _dbContext.SaveChanges();
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

        var id = AccountStore.NormalizeAccountId(accountId);
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