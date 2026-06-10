using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Auth;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加微信账户存储服务（使用文件存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="stateDir">状态目录路径，为null时使用默认目录</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWeixinFileStorage(
        this IServiceCollection services,
        string? stateDir = null)
    {
        services.AddSingleton<IAccountStore>(sp =>
        {
            var logger = sp.GetService<ILogger<AccountStore>>();
            return new AccountStore(stateDir, logger);
        });

        return services;
    }

    /// <summary>
    /// 添加微信账户存储服务（默认使用文件存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="stateDir">状态目录路径，为null时使用默认目录</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWeixinStorage(
        this IServiceCollection services,
        string? stateDir = null)
    {
        return services.AddWeixinFileStorage(stateDir);
    }
}