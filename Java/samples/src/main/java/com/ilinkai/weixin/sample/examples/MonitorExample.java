package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.messaging.MessageMonitorService;

import java.io.File;
import java.util.List;

public class MonitorExample extends ExampleBase {
    @Override public String getName() { return "monitor"; }
    @Override public String getDescription() { return "监控消息示例"; }
    @Override public String getUsage() { return "monitor --account-id <账户ID> --token <令牌>"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("监控消息示例");

        String accountId = parseArgument(args, "--account-id");
        String token = parseArgument(args, "--token");

        AccountStore accountStore = new AccountStore();

        if (accountId == null || accountId.isEmpty()) {
            List<String> accounts = accountStore.listAccountIds();
            if (!accounts.isEmpty()) {
                accountId = accounts.get(0);
                System.out.println("使用账户: " + accountId);
            }
        }

        if (accountId == null || accountId.isEmpty()) {
            printError("未指定账户ID，请使用 --account-id 参数或先登录");
            return 1;
        }

        AccountStore.ResolvedWeixinAccount account = accountStore.resolveAccount(accountId);
        if (token == null || token.isEmpty()) token = account.getToken();

        if (token == null || token.isEmpty()) {
            printError("未找到认证令牌，请先登录");
            return 1;
        }

        MessageMonitorService.MessageMonitorOptions options = new MessageMonitorService.MessageMonitorOptions();
        options.baseUrl = account.getBaseUrl();
        options.cdnBaseUrl = account.getCdnBaseUrl();
        options.token = token;
        options.accountId = account.getAccountId();
        options.mediaDir = System.getProperty("java.io.tmpdir") + File.separator + "ilinkai-weixin" + File.separator + "media";

        MessageMonitorService monitor = new MessageMonitorService(options);

        monitor.setOnMessageReceived(ctx -> {
            System.out.println();
            System.out.println("========================================");
            System.out.println("收到消息 [" + java.time.LocalTime.now().toString().substring(0, 8) + "]");
            System.out.println("  发送者: " + ctx.getFrom());
            System.out.println("  内容: " + ctx.getBody());
            String ct = ctx.getContextToken();
            if (ct != null && ct.length() > 20) ct = ct.substring(0, 20) + "...";
            System.out.println("  上下文令牌: " + ct);
            if (ctx.getMediaPath() != null && !ctx.getMediaPath().isEmpty()) {
                System.out.println("  媒体文件: " + ctx.getMediaPath());
                System.out.println("  媒体类型: " + ctx.getMediaType());
            }
            System.out.println("========================================");
        });

        monitor.setOnError(err -> printError("错误: " + err.getMessage()));

        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            System.out.println("\n正在停止监控...");
            monitor.stop();
        }));

        System.out.println("开始监控消息，按 Ctrl+C 停止...");
        System.out.println();

        try {
            monitor.start();
        } catch (InterruptedException e) {
            System.out.println("监控已停止");
        }
        return 0;
    }
}
