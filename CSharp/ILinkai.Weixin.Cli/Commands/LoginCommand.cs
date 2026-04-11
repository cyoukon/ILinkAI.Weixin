using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using QRCoder;

namespace ILinkai.Weixin.Cli.Commands;

public class LoginCommand : CommandBase
{
    public override string Name => "login";
    public override string Description => "扫码登录微信";

    public override async Task<int> ExecuteAsync(string[] args)
    {
        var baseUrl = ParseArgument(args, "--base-url");
        var routeTag = ParseArgument(args, "--route-tag");

        var apiBaseUrl = baseUrl ?? WeixinApiClient.DefaultBaseUrl;
        var loginService = new QRCodeLoginService(apiBaseUrl, routeTag);

        Log("正在获取登录二维码...");
        var startResult = await loginService.StartLoginAsync();

        if (string.IsNullOrEmpty(startResult.QRCodeUrl))
        {
            Error("获取二维码失败: " + startResult.Message);
            return 1;
        }

        Log("\n使用微信扫描以下二维码，以完成连接：\n");
        PrintQRCode(startResult.QRCodeUrl);

        Log("\n等待连接结果...\n");

        var waitResult = await loginService.WaitForLoginAsync(
            startResult.QRCode,
            timeoutMs: 480_000,
            onStatusChanged: (status) => Console.Write(status),
            onQRRefreshed: (url) => PrintQRCode(url));

        if (waitResult.Connected)
        {
            var normalizedId = AccountStore.NormalizeAccountId(waitResult.AccountId ?? "");
            AccountStore.SaveAccount(normalizedId, new WeixinAccountData
            {
                Token = waitResult.BotToken,
                BaseUrl = waitResult.BaseUrl,
                UserId = waitResult.UserId
            });
            AccountStore.RegisterAccountId(normalizedId);

            Log($"\n✅ 登录成功！");
            Log($"账户ID: {normalizedId}");
            Log($"Token: {waitResult.BotToken?.Substring(0, Math.Min(20, waitResult.BotToken?.Length ?? 0))}...");
            return 0;
        }
        else
        {
            Error(waitResult.Message);
            return 1;
        }
    }

    private static void PrintQRCode(string content)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeString = qrCode.GetGraphic(1);
            Console.WriteLine(qrCodeString);
        }
        catch
        {
            Console.WriteLine($"二维码链接: {content}");
        }
    }
}
