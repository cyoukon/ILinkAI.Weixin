package com.ilinkai.weixin.sample.examples;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiClientOptions;
import com.ilinkai.weixin.auth.AccountStore;
import com.ilinkai.weixin.messaging.MessageMonitorService;
import com.ilinkai.weixin.messaging.MessageSendService;
import com.ilinkai.weixin.models.*;
import com.ilinkai.weixin.sample.LlmService;

import java.io.File;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Scanner;

public class FullWorkflowExample extends ExampleBase {
    private final LoginExample loginExample = new LoginExample();

    @Override public String getName() { return "full"; }
    @Override public String getDescription() { return "完整工作流示例"; }
    @Override public String getUsage() { return "full"; }

    @Override
    public int execute(String[] args) throws Exception {
        printHeader("完整工作流示例");
        System.out.println("此示例演示完整的登录->监控->回复流程");
        System.out.println();

        AccountStore accountStore = new AccountStore();
        List<String> accounts = accountStore.listAccountIds();
        String accountId;

        Scanner scanner = new Scanner(System.in);

        if (accounts.isEmpty()) {
            System.out.println("没有已登录账户，开始登录流程...");
            System.out.println();
            int loginResult = loginExample.execute(args);
            if (loginResult != 0) return loginResult;
            accounts = accountStore.listAccountIds();
            accountId = accounts.get(accounts.size() - 1);
        } else {
            System.out.println("请选择账户:");
            System.out.println("  [0] 新登录账户");
            for (int i = 0; i < accounts.size(); i++) {
                System.out.println("  [" + (i + 1) + "] " + accounts.get(i));
            }
            System.out.println();

            int selection = -1;
            while (selection < 0 || selection > accounts.size()) {
                System.out.print("请输入选择 (0-" + accounts.size() + "): ");
                try { selection = Integer.parseInt(scanner.nextLine().trim()); } catch (Exception e) { selection = -1; }
            }

            if (selection == 0) {
                System.out.println();
                int loginResult = loginExample.execute(args);
                if (loginResult != 0) return loginResult;
                accounts = accountStore.listAccountIds();
                accountId = accounts.get(accounts.size() - 1);
            } else {
                accountId = accounts.get(selection - 1);
            }
        }

        AccountStore.ResolvedWeixinAccount account = accountStore.resolveAccount(accountId);
        System.out.println("使用账户: " + accountId);
        System.out.println();

        // 选择回复模式
        boolean useLlm = false;
        LlmService llmService = null;

        System.out.println("选择回复模式:");
        System.out.println("  [1] Echo 模式 - 回显收到的消息详情");
        System.out.println("  [2] LLM 模式 - 调用大模型对话接口回复");
        System.out.print("请选择 (1/2, 默认 1): ");
        String modeInput = scanner.nextLine().trim();

        if ("2".equals(modeInput)) {
            LlmService.LlmConfig config = new LlmService.LlmConfig();
            System.out.print("API Base URL (默认 " + config.baseUrl + "): ");
            String baseUrl = scanner.nextLine().trim();
            if (!baseUrl.isEmpty()) config.baseUrl = baseUrl;

            System.out.print("API Key: ");
            String apiKey = scanner.nextLine().trim();
            if (apiKey.isEmpty()) {
                System.out.println("API Key 不能为空，回退到 Echo 模式。");
            } else {
                config.apiKey = apiKey;
                System.out.print("模型名称 (默认 " + config.model + "): ");
                String model = scanner.nextLine().trim();
                if (!model.isEmpty()) config.model = model;

                System.out.print("系统提示词 (可选，回车跳过): ");
                String prompt = scanner.nextLine().trim();
                if (!prompt.isEmpty()) config.systemPrompt = prompt;

                llmService = new LlmService(config);
                useLlm = true;
            }
        }

        WeixinApiClientOptions opts = new WeixinApiClientOptions();
        opts.setBaseUrl(account.getBaseUrl());
        opts.setToken(account.getToken());
        WeixinApiClient apiClient = new WeixinApiClient(opts);
        MessageSendService sendService = new MessageSendService(apiClient, account.getCdnBaseUrl());

        MessageMonitorService.MessageMonitorOptions monOpts = new MessageMonitorService.MessageMonitorOptions();
        monOpts.baseUrl = account.getBaseUrl();
        monOpts.cdnBaseUrl = account.getCdnBaseUrl();
        monOpts.token = account.getToken();
        monOpts.accountId = account.getAccountId();
        monOpts.mediaDir = System.getProperty("java.io.tmpdir") + File.separator + "ilinkai-weixin" + File.separator + "media";

        MessageMonitorService monitor = new MessageMonitorService(monOpts);
        final boolean finalUseLlm = useLlm;
        final LlmService finalLlmService = llmService;

        monitor.setOnMessageReceived(ctx -> {
            System.out.println();
            System.out.println("========================================");
            System.out.println("收到消息: " + ctx.getBody());
            System.out.println("发送者: " + ctx.getFrom());

            if (ctx.getContextToken() == null || ctx.getContextToken().isEmpty()) {
                System.out.println("⚠ 无 ContextToken，跳过回复");
            } else {
                try {
                    String reply;
                    if (finalUseLlm && finalLlmService != null) {
                        if (ctx.getBody() == null || ctx.getBody().trim().isEmpty()) {
                            reply = "(收到非文本消息，暂不支持 LLM 处理)";
                        } else {
                            System.out.println("   🤖 正在调用大模型...");
                            reply = finalLlmService.chat(ctx.getBody());
                        }
                    } else {
                        reply = formatEchoReply(ctx.getOriginalMessage());
                    }
                    sendService.sendText(ctx.getFrom(), reply, ctx.getContextToken());
                    System.out.println("已自动回复：" + reply);
                } catch (Exception ex) {
                    System.out.println("回复失败: " + ex.getMessage());
                }
            }
            System.out.println("========================================");
        });

        monitor.setOnError(err -> System.out.println("错误: " + err.getMessage()));

        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            monitor.stop();
        }));

        System.out.println("开始监控并自动回复消息，按 Ctrl+C 停止...");
        System.out.println();

        try {
            monitor.start();
        } catch (Exception e) {
            System.out.println("已停止");
        }
        return 0;
    }

    private String formatEchoReply(WeixinMessage msg) {
        if (msg == null) return "(空消息)";
        StringBuilder sb = new StringBuilder();
        sb.append("📋 消息详情:\n");
        sb.append("  消息ID: ").append(msg.getMessageId()).append("\n");
        sb.append("  发送者: ").append(msg.getFromUserId()).append("\n");
        sb.append("  接收者: ").append(msg.getToUserId()).append("\n");
        if (msg.getCreateTimeMs() != null) {
            String time = LocalDateTime.ofInstant(Instant.ofEpochMilli(msg.getCreateTimeMs()), ZoneId.systemDefault())
                    .format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss"));
            sb.append("  时间: ").append(time).append("\n");
        }
        if (msg.getItemList() != null) {
            for (MessageItem item : msg.getItemList()) {
                String typeName;
                int t = item.getType() != null ? item.getType() : 0;
                switch (t) {
                    case Enums.MessageItemType.TEXT: typeName = "文本"; break;
                    case Enums.MessageItemType.IMAGE: typeName = "图片"; break;
                    case Enums.MessageItemType.VOICE: typeName = "语音"; break;
                    case Enums.MessageItemType.FILE: typeName = "文件"; break;
                    case Enums.MessageItemType.VIDEO: typeName = "视频"; break;
                    default: typeName = "未知(" + t + ")";
                }
                sb.append("  [").append(typeName).append("] ").append(getItemPreview(item)).append("\n");
            }
        }
        return sb.toString().trim();
    }

    private String getItemPreview(MessageItem item) {
        int t = item.getType() != null ? item.getType() : 0;
        if (t == Enums.MessageItemType.TEXT)
            return item.getTextItem() != null ? item.getTextItem().getText() : "";
        if (t == Enums.MessageItemType.IMAGE && item.getImageItem() != null)
            return "尺寸: " + item.getImageItem().getThumbWidth() + "x" + item.getImageItem().getThumbHeight();
        if (t == Enums.MessageItemType.VOICE && item.getVoiceItem() != null)
            return "时长: " + item.getVoiceItem().getPlayTime() + "ms | 转文字: " + (item.getVoiceItem().getText() != null ? item.getVoiceItem().getText() : "无");
        if (t == Enums.MessageItemType.FILE && item.getFileItem() != null)
            return item.getFileItem().getFileName() + " (" + item.getFileItem().getLength() + " bytes)";
        if (t == Enums.MessageItemType.VIDEO && item.getVideoItem() != null)
            return "时长: " + item.getVideoItem().getPlayLength() + "s";
        return "";
    }
}
