using ILinkai.Weixin.Sdk.Messaging;

namespace ILinkai.Weixin.Cli.Commands;

public class MonitorCommand : CommandBase
{
    public override string Name => "monitor";
    public override string Description => "监控消息";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var token = ParseArgument(args, "--token");
        var baseUrl = ParseArgument(args, "--base-url");
        var accountId = ParseArgument(args, "--account-id");
        var mediaDir = ParseArgument(args, "--media-dir");

        if (string.IsNullOrEmpty(accountId))
        {
            Error("用法: monitor --account-id <账户ID> [--token <令牌>] [--base-url <URL>] [--media-dir <目录>]");
            return 1;
        }

        var account = AccountStore.ResolveAccount(accountId);

        var effectiveToken = token ?? account.Token;
        if (string.IsNullOrEmpty(effectiveToken))
        {
            Error("未找到认证令牌，请先登录");
            return 1;
        }

        var options = new MessageMonitorOptions
        {
            BaseUrl = baseUrl ?? account.BaseUrl,
            CdnBaseUrl = account.CdnBaseUrl,
            Token = effectiveToken,
            AccountId = account.AccountId,
            MediaDir = mediaDir ?? Path.Combine(Path.GetTempPath(), "ilinkai-weixin", "media")
        };

        using var monitor = new MessageMonitorService(options);

        monitor.MessageReceived += (sender, e) =>
        {
            Log($"收到消息: From={e.Context.From} Body={e.Context.Body.Substring(0, Math.Min(100, e.Context.Body.Length))}...");
            if (!string.IsNullOrEmpty(e.Context.MediaPath))
            {
                Log($"  媒体文件: {e.Context.MediaPath}");
            }
        };

        monitor.ErrorOccurred += (sender, e) =>
        {
            Error($"错误: {e.Error.Message}");
        };

        monitor.StatusChanged += (sender, e) =>
        {
            Log($"状态变化: Running={e.Running}");
        };

        Log($"开始监控账户: {accountId}");
        Log("按 Ctrl+C 停止监控");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await monitor.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log("监控已停止");
        }

        return 0;
    }
}
