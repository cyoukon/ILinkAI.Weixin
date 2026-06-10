namespace ILinkai.Weixin.Sdk.SqlServer.Models;

/// <summary>
/// 账户实体（EF Core 映射）
/// </summary>
public class AccountEntity
{
    /// <summary>
    /// 账户ID（主键）
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

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

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}