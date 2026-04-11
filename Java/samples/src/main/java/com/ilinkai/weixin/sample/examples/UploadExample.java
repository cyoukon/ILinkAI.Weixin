package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiClientOptions;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.media.MediaUploadService;

import java.io.File;
import java.util.List;

public class UploadExample extends ExampleBase {
    @Override public String getName() { return "upload"; }
    @Override public String getDescription() { return "上传文件示例"; }
    @Override public String getUsage() { return "upload --file <文件路径> --to <用户ID> --token <令牌>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("上传文件示例");

        String filePath = parseArgument(args, "--file");
        String toUserId = parseArgument(args, "--to");
        String token = parseArgument(args, "--token");

        if (filePath == null || toUserId == null || filePath.isEmpty() || toUserId.isEmpty()) {
            System.out.println("用法: " + getUsage());
            return 1;
        }

        if (!new File(filePath).exists()) {
            printError("文件不存在: " + filePath);
            return 1;
        }

        AccountStore accountStore = new AccountStore();
        List<String> accounts = accountStore.listAccountIds();
        if ((token == null || token.isEmpty()) && !accounts.isEmpty()) {
            AccountStore.WeixinAccountData account = accountStore.loadAccount(accounts.get(0));
            if (account != null) token = account.getToken();
        }
        if (token == null || token.isEmpty()) { printError("未找到认证令牌"); return 1; }

        WeixinApiClientOptions opts = new WeixinApiClientOptions();
        opts.setBaseUrl(WeixinApiClient.DEFAULT_BASE_URL);
        opts.setToken(token);
        WeixinApiClient apiClient = new WeixinApiClient(opts);
        MediaUploadService uploadService = new MediaUploadService(apiClient, WeixinApiClient.DEFAULT_CDN_BASE_URL);

        try {
            System.out.println("正在上传文件: " + filePath);
            MediaUploadService.UploadedFileInfo uploaded = uploadService.uploadImage(filePath, toUserId);
            printSuccess("上传成功！");
            System.out.println("  文件键: " + uploaded.fileKey);
            String dp = uploaded.downloadEncryptedQueryParam;
            System.out.println("  下载参数: " + dp.substring(0, Math.min(40, dp.length())) + "...");
            System.out.println("  文件大小: " + uploaded.fileSize + " 字节");
            System.out.println("  密文大小: " + uploaded.fileSizeCiphertext + " 字节");
            return 0;
        } catch (Exception ex) {
            printError("上传失败: " + ex.getMessage());
            return 1;
        }
    }
}
