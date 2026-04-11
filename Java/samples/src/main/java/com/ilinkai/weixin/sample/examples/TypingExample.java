package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiClientOptions;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.messaging.MessageSendService;

import java.util.List;

public class TypingExample extends ExampleBase {
    @Override public String getName() { return "typing"; }
    @Override public String getDescription() { return "发送输入状态示例"; }
    @Override public String getUsage() { return "typing --user-id <用户ID> --ticket <输入票据> --token <令牌>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("发送输入状态示例");

        String userId = parseArgument(args, "--user-id");
        String token = parseArgument(args, "--token");
        String typingTicket = parseArgument(args, "--ticket");

        if (userId == null || typingTicket == null || userId.isEmpty() || typingTicket.isEmpty()) {
            System.out.println("用法: " + getUsage());
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
        MessageSendService sendService = new MessageSendService(apiClient, WeixinApiClient.DEFAULT_CDN_BASE_URL);

        try {
            sendService.sendTyping(userId, typingTicket, true);
            printSuccess("输入状态已发送");
            Thread.sleep(3000);
            sendService.sendTyping(userId, typingTicket, false);
            printSuccess("已取消输入状态");
            return 0;
        } catch (Exception ex) {
            printError("发送失败: " + ex.getMessage());
            return 1;
        }
    }
}
