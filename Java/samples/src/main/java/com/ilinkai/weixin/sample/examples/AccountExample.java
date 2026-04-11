package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.auth.AccountStore;

import java.util.List;

public class AccountExample extends ExampleBase {
    @Override public String getName() { return "account"; }
    @Override public String getDescription() { return "账户管理示例"; }
    @Override public String getUsage() { return "account list"; }

    @Override
    public int execute(String[] args) {
        printHeader("账户管理示例");

        AccountStore accountStore = new AccountStore();

        if (args.length < 2 || !"list".equals(args[1])) {
            System.out.println("用法: " + getUsage());
            return 1;
        }

        List<String> accounts = accountStore.listAccountIds();

        if (accounts.isEmpty()) {
            System.out.println("没有已注册的账户");
            System.out.println("请先运行 'login' 命令进行登录");
            return 0;
        }

        System.out.println("已注册账户 (" + accounts.size() + " 个):");
        System.out.println();

        for (String id : accounts) {
            AccountStore.ResolvedWeixinAccount account = accountStore.resolveAccount(id);
            AccountStore.WeixinAccountData accountData = accountStore.loadAccount(id);

            System.out.println("账户ID: " + account.getAccountId());
            System.out.println("  基础URL: " + account.getBaseUrl());
            System.out.println("  CDN URL: " + account.getCdnBaseUrl());
            System.out.println("  已配置: " + account.isConfigured());
            System.out.println("  已启用: " + account.isEnabled());

            if (accountData != null && accountData.getToken() != null && !accountData.getToken().isEmpty()) {
                String t = accountData.getToken();
                System.out.println("  Token: " + t.substring(0, Math.min(20, t.length())) + "...");
            }
            if (accountData != null && accountData.getUserId() != null) {
                System.out.println("  用户ID: " + accountData.getUserId());
            }
            if (accountData != null && accountData.getSavedAt() != null) {
                System.out.println("  保存时间: " + accountData.getSavedAt());
            }
            System.out.println();
        }
        return 0;
    }
}
