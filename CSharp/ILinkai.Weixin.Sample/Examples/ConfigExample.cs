using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;

namespace ILinkai.Weixin.Sample.Examples;

public class ConfigExample : ExampleBase
{
    public override string Name => "config";
    public override string Description => "获取配置示例";
    public override string Usage => "config --user-id <用户ID> --token <令牌>";

    public ConfigExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("获取配置示例");

        var userId = ParseArgument(args, "--user-id");
        var token = ParseArgument(args, "--token");

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"用法: {Usage}");
            return 1;
        }

        var accountStore = new AccountStore();
        var accounts = accountStore.ListAccountIds();

        if (string.IsNullOrEmpty(token) && accounts.Count > 0)
        {
            var account = accountStore.LoadAccount(accounts[0]);
            token = account?.Token;
        }

        if (string.IsNullOrEmpty(token))
        {
            PrintError("未找到认证令牌");
            return 1;
        }

        var apiClient = new WeixinApiClient(new WeixinApiClientOptions
        {
            BaseUrl = WeixinApiClient.DefaultBaseUrl,
            Token = token
        }, LoggerFactory.CreateLogger<WeixinApiClient>());

        try
        {
            var config = await apiClient.GetConfigAsync(userId);

            Console.WriteLine("配置信息:");
            Console.WriteLine($"  返回码: {config.Ret}");
            Console.WriteLine($"  错误消息: {config.ErrMsg}");

            if (!string.IsNullOrEmpty(config.TypingTicket))
            {
                Console.WriteLine($"  输入票据: {config.TypingTicket.Substring(0, Math.Min(30, config.TypingTicket.Length))}...");
            }

            return 0;
        }
        catch (Exception ex)
        {
            PrintError($"获取配置失败: {ex.Message}");
            return 1;
        }
    }
}
