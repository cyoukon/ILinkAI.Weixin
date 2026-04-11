using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Auth;
using ILinkai.Weixin.Sdk.Media;

namespace ILinkai.Weixin.Sample.Examples;

public class UploadExample : ExampleBase
{
    public override string Name => "upload";
    public override string Description => "上传文件示例";
    public override string Usage => "upload --file <文件路径> --to <用户ID> --token <令牌>";

    public UploadExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("上传文件示例");

        var filePath = ParseArgument(args, "--file");
        var toUserId = ParseArgument(args, "--to");
        var token = ParseArgument(args, "--token");

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(toUserId))
        {
            Console.WriteLine($"用法: {Usage}");
            return 1;
        }

        if (!File.Exists(filePath))
        {
            PrintError($"文件不存在: {filePath}");
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

        var uploadService = new MediaUploadService(
            apiClient,
            WeixinApiClient.DefaultCdnBaseUrl,
            LoggerFactory.CreateLogger<MediaUploadService>());

        try
        {
            Console.WriteLine($"正在上传文件: {filePath}");
            var uploaded = await uploadService.UploadImageAsync(filePath, toUserId);

            PrintSuccess("上传成功！");
            Console.WriteLine($"  文件键: {uploaded.FileKey}");
            Console.WriteLine($"  下载参数: {uploaded.DownloadEncryptedQueryParam.Substring(0, Math.Min(40, uploaded.DownloadEncryptedQueryParam.Length))}...");
            Console.WriteLine($"  文件大小: {uploaded.FileSize} 字节");
            Console.WriteLine($"  密文大小: {uploaded.FileSizeCiphertext} 字节");

            return 0;
        }
        catch (Exception ex)
        {
            PrintError($"上传失败: {ex.Message}");
            return 1;
        }
    }
}
