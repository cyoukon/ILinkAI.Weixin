using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.Messaging;

namespace ILinkai.Weixin.Sample.Examples;

public class TypingExample : ExampleBase
{
    public override string Name => "typing";
    public override string Description => "发送输入状态示例";
    public override string Usage => "typing --user-id <用户ID> --ticket <输入票据> --token <令牌>";

    public TypingExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("发送输入状态示例");

        var userId = ParseArgument(args, "--user-id");
        var token = ParseArgument(args, "--token");
        var typingTicket = ParseArgument(args, "--ticket");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(typingTicket))
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

        var sendService = new MessageSendService(
            apiClient,
            WeixinApiClient.DefaultCdnBaseUrl,
            LoggerFactory.CreateLogger<MessageSendService>());

        try
        {
            await sendService.SendTypingAsync(userId, typingTicket, isTyping: true);
            PrintSuccess("输入状态已发送");

            await Task.Delay(3000);

            await sendService.SendTypingAsync(userId, typingTicket, isTyping: false);
            PrintSuccess("已取消输入状态");

            return 0;
        }
        catch (Exception ex)
        {
            PrintError($"发送失败: {ex.Message}");
            return 1;
        }
    }
}
