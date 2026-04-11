using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sample.Helpers;

namespace ILinkai.Weixin.Sample.Examples;

public class LoginExample : ExampleBase
{
    public override string Name => "login";
    public override string Description => "扫码登录示例";
    public override string Usage => "login";

    public LoginExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("扫码登录示例");

        var loginService = new QRCodeLoginService(
            WeixinApiClient.DefaultBaseUrl,
            logger: LoggerFactory.CreateLogger<QRCodeLoginService>());

        Console.WriteLine("正在获取登录二维码...");
        var startResult = await loginService.StartLoginAsync();

        if (string.IsNullOrEmpty(startResult.QRCodeUrl))
        {
            PrintError($"获取二维码失败: {startResult.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("请使用微信扫描以下二维码登录:");
        ConsoleQRCodeHelper.DisplayQRCodeWithBorder(startResult.QRCodeUrl, "微信扫码登录");
        Console.WriteLine();
        Console.WriteLine("等待扫码...");

        var waitResult = await loginService.WaitForLoginAsync(
            startResult.QRCode,
            timeoutMs: 300000,
            onStatusChanged: (status) =>
            {
                if (status == ".")
                {
                    Console.Write(".");
                }
                else
                {
                    Console.WriteLine(status);
                }
            },
            onQRRefreshed: (url) =>
            {
                Console.WriteLine();
                Console.WriteLine("二维码已刷新:");
                ConsoleQRCodeHelper.DisplayQRCodeWithBorder(url, "微信扫码登录 (已刷新)");
            });

        if (waitResult.Connected)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            PrintSuccess("登录成功！");
            Console.WriteLine($"账户ID: {waitResult.AccountId}");
            Console.WriteLine($"用户ID: {waitResult.UserId}");
            Console.WriteLine($"Token: {waitResult.BotToken?.Substring(0, Math.Min(30, waitResult.BotToken?.Length ?? 0))}...");
            Console.WriteLine("========================================");

            var accountStore = new AccountStore();
            var normalizedId = AccountStore.NormalizeAccountId(waitResult.AccountId ?? "");
            accountStore.SaveAccount(normalizedId, new WeixinAccountData
            {
                Token = waitResult.BotToken,
                BaseUrl = waitResult.BaseUrl,
                UserId = waitResult.UserId
            });
            accountStore.RegisterAccountId(normalizedId);

            Console.WriteLine($"账户已保存: {normalizedId}");
            return 0;
        }
        else
        {
            Console.WriteLine();
            PrintError($"登录失败: {waitResult.Message}");
            return 1;
        }
    }
}
