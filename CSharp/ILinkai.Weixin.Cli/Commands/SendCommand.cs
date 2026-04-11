using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Messaging;

namespace ILinkai.Weixin.Cli.Commands;

public class SendCommand : CommandBase
{
    public override string Name => "send";
    public override string Description => "发送消息";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var to = ParseArgument(args, "--to");
        var text = ParseArgument(args, "--text");
        var token = ParseArgument(args, "--token");
        var baseUrl = ParseArgument(args, "--base-url");
        var contextToken = ParseArgument(args, "--context-token");

        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(text))
        {
            Error("用法: send --to <用户ID> --text <消息内容> --context-token <上下文令牌> [--token <令牌>] [--base-url <URL>]");
            return 1;
        }

        if (string.IsNullOrEmpty(contextToken))
        {
            Error("context-token 是必需的参数");
            return 1;
        }

        var account = AccountStore.ListAccountIds().FirstOrDefault();

        if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(account))
        {
            var accountData = AccountStore.LoadAccount(account);
            token = accountData?.Token;
        }

        if (string.IsNullOrEmpty(token))
        {
            Error("未找到认证令牌，请先登录");
            return 1;
        }

        var apiBaseUrl = baseUrl ?? WeixinApiClient.DefaultBaseUrl;

        var apiClient = new WeixinApiClient(new WeixinApiClientOptions
        {
            BaseUrl = apiBaseUrl,
            Token = token
        });

        var sendService = new MessageSendService(apiClient, WeixinApiClient.DefaultCdnBaseUrl);

        try
        {
            var messageId = await sendService.SendTextAsync(to, text, contextToken);
            Log($"消息发送成功，ID: {messageId}");
            return 0;
        }
        catch (Exception err)
        {
            Error($"发送失败: {err.Message}");
            return 1;
        }
    }
}
