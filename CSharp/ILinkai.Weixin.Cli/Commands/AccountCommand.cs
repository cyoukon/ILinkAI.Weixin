namespace ILinkai.Weixin.Cli.Commands;

public class AccountCommand : CommandBase
{
    public override string Name => "account";
    public override string Description => "账户管理";

    public override Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            PrintAccountUsage();
            return Task.FromResult(1);
        }

        var subCommand = args[1].ToLowerInvariant();

        return subCommand switch
        {
            "list" => Task.FromResult(ListAccounts()),
            "show" => ShowAccount(args),
            _ => Task.FromResult(PrintAccountUsage())
        };
    }

    private int ListAccounts()
    {
        var accounts = AccountStore.ListAccountIds();

        if (accounts.Count == 0)
        {
            Log("没有已注册的账户");
            return 0;
        }

        Log("已注册的账户:");
        foreach (var accountId in accounts)
        {
            var account = AccountStore.LoadAccount(accountId);
            var configured = !string.IsNullOrEmpty(account?.Token) ? "已配置" : "未配置";
            Log($"  - {accountId} [{configured}]");
        }

        return 0;
    }

    private Task<int> ShowAccount(string[] args)
    {
        if (args.Length < 3)
        {
            Error("用法: account show <账户ID>");
            return Task.FromResult(1);
        }

        var accountId = args[2];
        var account = AccountStore.ResolveAccount(accountId);

        Log($"账户ID: {account.AccountId}");
        Log($"基础URL: {account.BaseUrl}");
        Log($"CDN URL: {account.CdnBaseUrl}");
        Log($"已配置: {account.Configured}");
        Log($"已启用: {account.Enabled}");

        if (!string.IsNullOrEmpty(account.Token))
        {
            Log($"Token: {account.Token.Substring(0, Math.Min(20, account.Token.Length))}...");
        }

        return Task.FromResult(0);
    }

    private int PrintAccountUsage()
    {
        Console.WriteLine("用法: account <命令>");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  list              - 列出所有账户");
        Console.WriteLine("  show <账户ID>     - 显示账户详情");
        return 0;
    }
}
