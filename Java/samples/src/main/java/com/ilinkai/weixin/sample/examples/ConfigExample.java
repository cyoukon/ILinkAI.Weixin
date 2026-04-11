package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiClientOptions;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.models.ApiModels;

import java.util.List;

public class ConfigExample extends ExampleBase {
    @Override public String getName() { return "config"; }
    @Override public String getDescription() { return "获取配置示例"; }
    @Override public String getUsage() { return "config --user-id <用户ID> --token <令牌>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("获取配置示例");

        String userId = parseArgument(args, "--user-id");
        String token = parseArgument(args, "--token");

        if (userId == null || userId.isEmpty()) {
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

        try {
            ApiModels.GetConfigResponse config = apiClient.getConfig(userId, null);
            System.out.println("配置信息:");
            System.out.println("  返回码: " + config.getRet());
            System.out.println("  错误消息: " + config.getErrMsg());
            String ticket = config.getTypingTicket();
            if (ticket != null && !ticket.isEmpty()) {
                System.out.println("  输入票据: " + ticket.substring(0, Math.min(30, ticket.length())) + "...");
            }
            return 0;
        } catch (Exception ex) {
            printError("获取配置失败: " + ex.getMessage());
            return 1;
        }
    }
}
