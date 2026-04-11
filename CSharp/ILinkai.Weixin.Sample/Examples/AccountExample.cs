using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk.Auth;

namespace ILinkai.Weixin.Sample.Examples;

public class AccountExample : ExampleBase
{
    public override string Name => "account";
    public override string Description => "账户管理示例";
    public override string Usage => "account list";

    public AccountExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("账户管理示例");

        var accountStore = new AccountStore();

        if (args.Length < 2 || args[1] != "list")
        {
            Console.WriteLine($"用法: {Usage}");
            return Task.FromResult(1);
        }

        var accounts = accountStore.ListAccountIds();

        if (accounts.Count == 0)
        {
            Console.WriteLine("没有已注册的账户");
            Console.WriteLine("请先运行 'login' 命令进行登录");
            return Task.FromResult(0);
        }

        Console.WriteLine($"已注册账户 ({accounts.Count} 个):");
        Console.WriteLine();

        foreach (var id in accounts)
        {
            var account = accountStore.ResolveAccount(id);
            var accountData = accountStore.LoadAccount(id);

            Console.WriteLine($"账户ID: {account.AccountId}");
            Console.WriteLine($"  基础URL: {account.BaseUrl}");
            Console.WriteLine($"  CDN URL: {account.CdnBaseUrl}");
            Console.WriteLine($"  已配置: {account.Configured}");
            Console.WriteLine($"  已启用: {account.Enabled}");

            if (!string.IsNullOrEmpty(accountData?.Token))
            {
                Console.WriteLine($"  Token: {accountData.Token.Substring(0, Math.Min(20, accountData.Token.Length))}...");
            }

            if (!string.IsNullOrEmpty(accountData?.UserId))
            {
                Console.WriteLine($"  用户ID: {accountData.UserId}");
            }

            if (accountData?.SavedAt != null)
            {
                Console.WriteLine($"  保存时间: {accountData.SavedAt}");
            }

            Console.WriteLine();
        }

        return Task.FromResult(0);
    }
}
