using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.Messaging;

namespace ILinkai.Weixin.Sample.Examples;

public class MonitorExample : ExampleBase
{
    public override string Name => "monitor";
    public override string Description => "监控消息示例";
    public override string Usage => "monitor --account-id <账户ID> --token <令牌>";

    public MonitorExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("监控消息示例");

        var accountId = ParseArgument(args, "--account-id");
        var token = ParseArgument(args, "--token");

        var accountStore = new AccountStore();

        if (string.IsNullOrEmpty(accountId))
        {
            var accounts = accountStore.ListAccountIds();
            if (accounts.Count > 0)
            {
                accountId = accounts[0];
                Console.WriteLine($"使用账户: {accountId}");
            }
        }

        if (string.IsNullOrEmpty(accountId))
        {
            PrintError("未指定账户ID，请使用 --account-id 参数或先登录");
            return 1;
        }

        var account = accountStore.ResolveAccount(accountId);
        token ??= account.Token;

        if (string.IsNullOrEmpty(token))
        {
            PrintError("未找到认证令牌，请先登录");
            return 1;
        }

        var options = new MessageMonitorOptions
        {
            BaseUrl = account.BaseUrl,
            CdnBaseUrl = account.CdnBaseUrl,
            Token = token,
            AccountId = account.AccountId,
            MediaDir = Path.Combine(Path.GetTempPath(), "ilinkai-weixin", "media")
        };

        using var monitor = new MessageMonitorService(
            options,
            LoggerFactory.CreateLogger<MessageMonitorService>());

        monitor.MessageReceived += (sender, e) =>
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine($"收到消息 [{DateTime.Now:HH:mm:ss}]");
            Console.WriteLine($"  发送者: {e.Context.From}");
            Console.WriteLine($"  内容: {e.Context.Body}");
            Console.WriteLine($"  上下文令牌: {e.Context.ContextToken?.Substring(0, Math.Min(20, e.Context.ContextToken?.Length ?? 0))}...");

            if (!string.IsNullOrEmpty(e.Context.MediaPath))
            {
                Console.WriteLine($"  媒体文件: {e.Context.MediaPath}");
                Console.WriteLine($"  媒体类型: {e.Context.MediaType}");
            }
            Console.WriteLine("========================================");
        };

        monitor.ErrorOccurred += (sender, e) =>
        {
            PrintError($"错误: {e.Error.Message}");
        };

        monitor.StatusChanged += (sender, e) =>
        {
            if (e.Running)
            {
                Console.WriteLine($"监控已启动，账户: {e.AccountId}");
            }
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n正在停止监控...");
            cts.Cancel();
        };

        Console.WriteLine("开始监控消息，按 Ctrl+C 停止...");
        Console.WriteLine();

        try
        {
            await monitor.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("监控已停止");
        }

        return 0;
    }
}
