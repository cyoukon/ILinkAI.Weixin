using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.Messaging;
using ILinkai.Weixin.Sdk.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ILinkai.Weixin.Sample.Examples;

public class FullWorkflowExample : ExampleBase
{
    private readonly LoginExample _loginExample;
    private static readonly ConcurrentDictionary<string, string> _typingTicketCache = new();

    public override string Name => "full";
    public override string Description => "完整工作流示例";
    public override string Usage => "full";

    public FullWorkflowExample(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _loginExample = new LoginExample(loggerFactory);
    }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("完整工作流示例");
        Console.WriteLine("此示例演示完整的登录->监控->回复流程");
        Console.WriteLine();

        var accountStore = new AccountStore();
        var accounts = accountStore.ListAccountIds();

        string accountId;
        if (accounts.Count == 0)
        {
            Console.WriteLine("没有已登录账户，开始登录流程...");
            Console.WriteLine();

            var loginResult = await _loginExample.ExecuteAsync(args);
            if (loginResult != 0) return loginResult;

            accounts = accountStore.ListAccountIds();
            accountId = accounts[^1];
        }
        else
        {
            Console.WriteLine("请选择账户:");
            Console.WriteLine($"  [0] 新登录账户");
            int i = 0;
            for (; i < accounts.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {accounts[i]}");
            }
            Console.WriteLine($"  [{i + 1}] 清理账户");
            Console.WriteLine();

            int selection;
            do
            {
                Console.Write($"请输入选择 (0-{accounts.Count + 1}): ");
            } while (!int.TryParse(Console.ReadLine(), out selection) || selection < 0 || selection > accounts.Count + 1);

            if (selection == 0)
            {
                Console.WriteLine();
                var loginResult = await _loginExample.ExecuteAsync(args);
                if (loginResult != 0) return loginResult;

                accounts = accountStore.ListAccountIds();
                accountId = accounts[^1];
            }
            else if (selection > 0 && selection <= accounts.Count)
            {
                accountId = accounts[selection - 1];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("请选择要清理的账户:");
                for (i = 0; i < accounts.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {accounts[i]}");
                }
                Console.WriteLine();
                int clearSelection;
                do
                {
                    Console.Write($"请选择要清理的账户 (1-{accounts.Count}): ");
                } while (!int.TryParse(Console.ReadLine(), out clearSelection) || clearSelection < 1 || clearSelection > accounts.Count);
                var accountToClear = accounts[clearSelection - 1];
                accountStore.ClearAccount(accountToClear);
                Console.WriteLine($"已清理账户: {accountToClear}");
                return 0;
            }
        }

        var account = accountStore.ResolveAccount(accountId);

        Console.WriteLine($"使用账户: {accountId}");
        Console.WriteLine();

        // ── 选择回复模式 ──
        var (replyMode, llmService) = ConfigureReplyMode();

        var apiClient = new WeixinApiClient(new WeixinApiClientOptions
        {
            BaseUrl = account.BaseUrl,
            Token = account.Token
        }, LoggerFactory.CreateLogger<WeixinApiClient>());

        var sendService = new MessageSendService(
            apiClient,
            account.CdnBaseUrl,
            LoggerFactory.CreateLogger<MessageSendService>());

        var options = new MessageMonitorOptions
        {
            BaseUrl = account.BaseUrl,
            CdnBaseUrl = account.CdnBaseUrl,
            Token = account.Token,
            AccountId = account.AccountId,
            MediaDir = Path.Combine(Path.GetTempPath(), "ilinkai-weixin", "media")
        };

        using var monitor = new MessageMonitorService(
            options,
            LoggerFactory.CreateLogger<MessageMonitorService>());

        monitor.MessageReceived += async (sender, e) =>
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine($"收到消息: {e.Context.Body}");
            Console.WriteLine($"发送者: {e.Context.From}");

            if (string.IsNullOrEmpty(e.Context.ContextToken))
            {
                Console.WriteLine("⚠ 无 ContextToken，跳过回复");
            }
            else
            {
                try
                {
                    var typingTicket = await GetTypingTicketAsync(apiClient, e.Context.From);
                    await sendService.SendTypingAsync(e.Context.From, typingTicket);
                    string reply;
                    if (replyMode == ReplyMode.Echo)
                    {
                        reply = FormatEchoReply(e.Message);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(e.Context.Body))
                        {
                            reply = "(收到非文本消息，暂不支持 LLM 处理)";
                        }
                        else
                        {
                            System.Console.WriteLine("   🤖 正在调用大模型...");
                            reply = await llmService!.ChatAsync(e.Context.Body);
                        }
                    }
                    await sendService.SendTextAsync(e.Context.From, reply, e.Context.ContextToken);
                    Console.WriteLine("已自动回复：" + reply);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"回复失败: {ex.Message}");
                }
            }
            Console.WriteLine("========================================");
        };

        monitor.ErrorOccurred += (sender, e) =>
        {
            Console.WriteLine($"错误: {e.Error.Message}");
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("开始监控并自动回复消息，按 Ctrl+C 停止...");
        Console.WriteLine();

        try
        {
            await monitor.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("已停止");
        }

        return 0;
    }

    private (ReplyMode mode, LlmService? llm) ConfigureReplyMode()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("选择回复模式:");
        System.Console.WriteLine("  [1] Echo 模式 - 回显收到的消息详情");
        System.Console.WriteLine("  [2] LLM 模式 - 调用大模型对话接口回复");
        System.Console.Write("请选择 (1/2, 默认 1): ");

        var input = System.Console.ReadLine()?.Trim();
        if (input == "2")
        {
            var config = new LlmConfig();

            System.Console.Write($"API Base URL (默认 {config.BaseUrl}): ");
            var baseUrl = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(baseUrl)) config.BaseUrl = baseUrl;

            System.Console.Write("API Key: ");
            string? apiKey = Environment.GetEnvironmentVariable("BIG_MODEL_KEY", EnvironmentVariableTarget.User);
            apiKey = string.IsNullOrEmpty(apiKey) ? System.Console.ReadLine()?.Trim() : apiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                System.Console.WriteLine("API Key 不能为空，回退到 Echo 模式。");
                return (ReplyMode.Echo, null);
            }
            config.ApiKey = apiKey;

            System.Console.Write($"模型名称 (默认 {config.Model}): ");
            var model = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(model)) config.Model = model;

            System.Console.Write("系统提示词 (可选，回车跳过): ");
            var prompt = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(prompt)) config.SystemPrompt = prompt;

            return (ReplyMode.Llm, new LlmService(config));
        }

        return (ReplyMode.Echo, null);
    }

    private string FormatEchoReply(WeixinMessage msg)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 消息详情:");
        sb.AppendLine($"  消息ID: {msg.MessageId}");
        sb.AppendLine($"  发送者: {msg.FromUserId}");
        sb.AppendLine($"  接收者: {msg.ToUserId}");
        sb.AppendLine($"  时间: {(msg.CreateTimeMs.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(msg.CreateTimeMs.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "未知")}");

        if (msg.ItemList != null)
        {
            foreach (var item in msg.ItemList)
            {
                var typeName = item.Type switch
                {
                    (int)MessageItemType.Text => "文本",
                    (int)MessageItemType.Image => "图片",
                    (int)MessageItemType.Voice => "语音",
                    (int)MessageItemType.File => "文件",
                    (int)MessageItemType.Video => "视频",
                    _ => $"未知({item.Type})",
                };
                sb.AppendLine($"  [{typeName}] {GetItemPreview(item)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string GetItemPreview(MessageItem item)
    {
        if (item.Type == (int)MessageItemType.Text)
            return item.TextItem?.Text ?? "";
        if (item.Type == (int)MessageItemType.Image)
            return $"尺寸: {item.ImageItem?.ThumbWidth}x{item.ImageItem?.ThumbHeight}";
        if (item.Type == (int)MessageItemType.Voice)
            return $"时长: {item.VoiceItem?.PlayTime}ms | 转文字: {item.VoiceItem?.Text ?? "无"}";
        if (item.Type == (int)MessageItemType.File)
            return $"{item.FileItem?.FileName} ({item.FileItem?.Length} bytes)";
        if (item.Type == (int)MessageItemType.Video)
            return $"时长: {item.VideoItem?.PlayLength}s";
        return "";
    }

    private async Task<string> GetTypingTicketAsync(WeixinApiClient apiClient, string userId)
    {
        if (_typingTicketCache.TryGetValue(userId, out var cached))
            return cached;

        var config = await apiClient.GetConfigAsync(userId);
        var ticket = config?.TypingTicket ?? string.Empty;
        _typingTicketCache[userId] = ticket;
        return ticket;
    }

    enum ReplyMode { Echo, Llm }
}
