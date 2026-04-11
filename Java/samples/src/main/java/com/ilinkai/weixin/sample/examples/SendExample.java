package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiClientOptions;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.messaging.MessageSendService;

import java.util.List;

public class SendExample extends ExampleBase {
    @Override public String getName() { return "send"; }
    @Override public String getDescription() { return "发送消息示例"; }
    @Override public String getUsage() { return "send --to <用户ID> --text <消息内容> --token <令牌> --context-token <上下文令牌>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("发送消息示例");

        String to = parseArgument(args, "--to");
        String text = parseArgument(args, "--text");
        String token = parseArgument(args, "--token");
        String contextToken = parseArgument(args, "--context-token");

        if (to == null || to.isEmpty() || text == null || text.isEmpty()) {
            System.out.println("用法: " + getUsage());
            return 1;
        }

        AccountStore accountStore = new AccountStore();
        List<String> accounts = accountStore.listAccountIds();

        if ((token == null || token.isEmpty()) && !accounts.isEmpty()) {
            AccountStore.WeixinAccountData account = accountStore.loadAccount(accounts.get(0));
            if (account != null) token = account.getToken();
            System.out.println("使用已保存的账户: " + accounts.get(0));
        }

        if (token == null || token.isEmpty()) {
            printError("未找到认证令牌，请先登录或使用 --token 参数");
            return 1;
        }

        if (contextToken == null || contextToken.isEmpty()) {
            printWarning("未提供 context-token，消息可能无法正确关联到会话");
            contextToken = "";
        }

        WeixinApiClientOptions opts = new WeixinApiClientOptions();
        opts.setBaseUrl(WeixinApiClient.DEFAULT_BASE_URL);
        opts.setToken(token);
        WeixinApiClient apiClient = new WeixinApiClient(opts);
        MessageSendService sendService = new MessageSendService(apiClient, WeixinApiClient.DEFAULT_CDN_BASE_URL);

        try {
            String messageId = sendService.sendText(to, text, contextToken);
            printSuccess("消息发送成功！ID: " + messageId);
            return 0;
        } catch (Exception ex) {
            printError("发送失败: " + ex.getMessage());
            return 1;
        }
    }
}
