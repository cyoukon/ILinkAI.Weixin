using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.Messaging;

namespace ILinkai.Weixin.Sample.Examples;

public class SendExample : ExampleBase
{
    public override string Name => "send";
    public override string Description => "发送消息示例";
    public override string Usage => "send --to <用户ID> --text <消息内容> --token <令牌> --context-token <上下文令牌>";

    public SendExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("发送消息示例");

        var to = ParseArgument(args, "--to");
        var text = ParseArgument(args, "--text");
        var token = ParseArgument(args, "--token");
        var contextToken = ParseArgument(args, "--context-token");

        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(text))
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
            Console.WriteLine($"使用已保存的账户: {accounts[0]}");
        }

        if (string.IsNullOrEmpty(token))
        {
            PrintError("未找到认证令牌，请先登录或使用 --token 参数");
            return 1;
        }

        if (string.IsNullOrEmpty(contextToken))
        {
            PrintWarning("未提供 context-token，消息可能无法正确关联到会话");
            contextToken = "";
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
            var messageId = await sendService.SendTextAsync(to, text, contextToken);
            PrintSuccess($"消息发送成功！ID: {messageId}");
            return 0;
        }
        catch (Exception ex)
        {
            PrintError($"发送失败: {ex.Message}");
            return 1;
        }
    }
}
