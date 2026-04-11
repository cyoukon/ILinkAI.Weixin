package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.auth.QRCodeLoginService;
import com.ilinkai.weixin.sample.helpers.ConsoleQRCodeHelper;

public class LoginExample extends ExampleBase {
    @Override public String getName() { return "login"; }
    @Override public String getDescription() { return "扫码登录示例"; }
    @Override public String getUsage() { return "login"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("扫码登录示例");

        QRCodeLoginService loginService = new QRCodeLoginService(WeixinApiClient.DEFAULT_BASE_URL);

        System.out.println("正在获取登录二维码...");
        QRCodeLoginService.QRLoginStartResult startResult = loginService.startLogin(null, null);

        if (startResult.qrCodeUrl == null || startResult.qrCodeUrl.isEmpty()) {
            printError("获取二维码失败: " + startResult.message);
            return 1;
        }

        System.out.println();
        System.out.println("请使用微信扫描以下二维码登录:");
        ConsoleQRCodeHelper.displayQRCodeWithBorder(startResult.qrCodeUrl, "微信扫码登录");
        System.out.println("等待扫码...");

        QRCodeLoginService.QRLoginWaitResult waitResult = loginService.waitForLogin(
                startResult.qrCode, 300000,
                status -> {
                    if (".".equals(status)) System.out.print(".");
                    else System.out.println(status);
                },
                url -> {
                    System.out.println();
                    System.out.println("二维码已刷新:");
                    ConsoleQRCodeHelper.displayQRCodeWithBorder(url, "微信扫码登录 (已刷新)");
                });

        if (waitResult.connected) {
            System.out.println();
            System.out.println("========================================");
            printSuccess("登录成功！");
            System.out.println("账户ID: " + waitResult.accountId);
            System.out.println("用户ID: " + waitResult.userId);
            if (waitResult.botToken != null) {
                System.out.println("Token: " + waitResult.botToken.substring(0, Math.min(30, waitResult.botToken.length())) + "...");
            }
            System.out.println("========================================");

            AccountStore accountStore = new AccountStore();
            String normalizedId = AccountStore.normalizeAccountId(waitResult.accountId != null ? waitResult.accountId : "");
            AccountStore.WeixinAccountData data = new AccountStore.WeixinAccountData();
            data.setToken(waitResult.botToken);
            data.setBaseUrl(waitResult.baseUrl);
            data.setUserId(waitResult.userId);
            accountStore.saveAccount(normalizedId, data);
            accountStore.registerAccountId(normalizedId);

            System.out.println("账户已保存: " + normalizedId);
            return 0;
        } else {
            System.out.println();
            printError("登录失败: " + waitResult.message);
            return 1;
        }
    }
}
