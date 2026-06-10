namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 账户存储接口
/// </summary>
public interface IAccountStore
{
    /// <summary>
    /// 列出所有已注册的账户ID
    /// </summary>
    List<string> ListAccountIds();

    /// <summary>
    /// 注册账户ID到索引
    /// </summary>
    void RegisterAccountId(string accountId);

    /// <summary>
    /// 加载账户数据
    /// </summary>
    WeixinAccountData? LoadAccount(string accountId);

    /// <summary>
    /// 保存账户数据
    /// </summary>
    void SaveAccount(string accountId, WeixinAccountData data);

    /// <summary>
    /// 清除账户数据
    /// </summary>
    void ClearAccount(string accountId);

    /// <summary>
    /// 解析账户信息
    /// </summary>
    ResolvedWeixinAccount ResolveAccount(string accountId);
}