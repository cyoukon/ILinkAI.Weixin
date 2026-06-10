using ILinkai.Weixin.Sdk.SqlServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ILinkai.Weixin.Sdk.SqlServer;

/// <summary>
/// 账户数据库上下文
/// </summary>
public class AccountDbContext : DbContext
{
    /// <summary>
    /// 账户表
    /// </summary>
    public DbSet<AccountEntity> Accounts { get; set; }

    /// <summary>
    /// 创建AccountDbContext实例
    /// </summary>
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    /// <summary>
    /// 配置模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.HasIndex(e => e.UserId);
        });
    }
}