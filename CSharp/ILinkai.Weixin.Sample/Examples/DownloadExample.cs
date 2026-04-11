using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Media;

namespace ILinkai.Weixin.Sample.Examples;

public class DownloadExample : ExampleBase
{
    public override string Name => "download";
    public override string Description => "下载文件示例";
    public override string Usage => "download --param <加密参数> --aes-key <AES密钥> --output <输出路径>";

    public DownloadExample(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    public override async Task<int> ExecuteAsync(string[] args)
    {
        PrintHeader("下载文件示例");

        var encryptParam = ParseArgument(args, "--param");
        var aesKey = ParseArgument(args, "--aes-key");
        var output = ParseArgument(args, "--output");

        if (string.IsNullOrEmpty(encryptParam))
        {
            Console.WriteLine($"用法: {Usage}");
            return 1;
        }

        output ??= Path.Combine(Path.GetTempPath(), "ilinkai-weixin", "downloaded_media.bin");

        var downloadService = new MediaDownloadService(
            WeixinApiClient.DefaultCdnBaseUrl,
            LoggerFactory.CreateLogger<MediaDownloadService>());

        try
        {
            Console.WriteLine("正在下载...");
            var data = await downloadService.DownloadImageAsync(encryptParam, aesKey);
            await downloadService.SaveMediaToFileAsync(data, Path.GetDirectoryName(output)!, Path.GetFileName(output));

            PrintSuccess("下载成功！");
            Console.WriteLine($"  保存路径: {output}");
            Console.WriteLine($"  文件大小: {data.Length} 字节");

            return 0;
        }
        catch (Exception ex)
        {
            PrintError($"下载失败: {ex.Message}");
            return 1;
        }
    }
}
