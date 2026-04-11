package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.media.MediaDownloadService;

import java.io.File;

public class DownloadExample extends ExampleBase {
    @Override public String getName() { return "download"; }
    @Override public String getDescription() { return "下载文件示例"; }
    @Override public String getUsage() { return "download --param <加密参数> --aes-key <AES密钥> --output <输出路径>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("下载文件示例");

        String encryptParam = parseArgument(args, "--param");
        String aesKey = parseArgument(args, "--aes-key");
        String output = parseArgument(args, "--output");

        if (encryptParam == null || encryptParam.isEmpty()) {
            System.out.println("用法: " + getUsage());
            return 1;
        }

        if (output == null || output.isEmpty()) {
            output = System.getProperty("java.io.tmpdir") + File.separator + "ilinkai-weixin" + File.separator + "downloaded_media.bin";
        }

        MediaDownloadService downloadService = new MediaDownloadService(WeixinApiClient.DEFAULT_CDN_BASE_URL);

        try {
            System.out.println("正在下载...");
            byte[] data = downloadService.downloadImage(encryptParam, aesKey);
            File outputFile = new File(output);
            downloadService.saveMediaToFile(data, outputFile.getParent(), outputFile.getName());
            printSuccess("下载成功！");
            System.out.println("  保存路径: " + output);
            System.out.println("  文件大小: " + data.length + " 字节");
            return 0;
        } catch (Exception ex) {
            printError("下载失败: " + ex.getMessage());
            return 1;
        }
    }
}
