using ILinkai.Weixin.Sdk.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ILinkai.Weixin.Sdk.SqlServer;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加微信账户存储服务（使用 SQL Server）
    /// 此方法会替换默认的文件存储实现
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWeixinSqlServerStorage(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AccountDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAccountStore, SqlServerAccountStore>();

        return services;
    }

    /// <summary>
    /// 添加微信账户存储服务（使用 SQL Server，自定义配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">DbContext配置操作</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWeixinSqlServerStorage(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<AccountDbContext>(optionsAction);
        services.AddScoped<IAccountStore, SqlServerAccountStore>();

        return services;
    }
}