package com.ilinkai.weixin.sample;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/**
 * 大模型对话服务，支持 OpenAI 兼容接口
 */
public class LlmService {
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private final LlmConfig config;

    public LlmService(LlmConfig config) {
        this.config = config;
    }

    public String chat(String userMessage) {
        String url = config.baseUrl.replaceAll("/+$", "") + "/v1/chat/completions";
        try {
            String systemPrompt = config.systemPrompt != null
                    ? config.systemPrompt : "你是一个处理提单的小助手。";
            String body = MAPPER.writeValueAsString(new java.util.LinkedHashMap<String, Object>() {{
                put("model", config.model);
                put("messages", new Object[]{
                    new java.util.LinkedHashMap<String, String>() {{ put("role", "system"); put("content", systemPrompt); }},
                    new java.util.LinkedHashMap<String, String>() {{ put("role", "user"); put("content", userMessage); }}
                });
                put("max_tokens", config.maxTokens);
                put("temperature", config.temperature);
            }});

            HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
            conn.setRequestMethod("POST");
            conn.setDoOutput(true);
            conn.setConnectTimeout(60000);
            conn.setReadTimeout(60000);
            conn.setRequestProperty("Content-Type", "application/json");
            conn.setRequestProperty("Authorization", "Bearer " + config.apiKey);

            byte[] bodyBytes = body.getBytes(StandardCharsets.UTF_8);
            try (OutputStream os = conn.getOutputStream()) {
                os.write(bodyBytes);
            }

            int status = conn.getResponseCode();
            String respBody;
            try (InputStream is_ = status >= 400 ? conn.getErrorStream() : conn.getInputStream()) {
                respBody = readStream(is_);
            }

            if (status < 200 || status >= 300) {
                return "[LLM 调用失败: HTTP " + status + "] " + truncate(respBody, 200);
            }

            JsonNode root = MAPPER.readTree(respBody);
            String content = root.path("choices").path(0).path("message").path("content").asText(null);
            return content != null ? content : "(空回复)";
        } catch (Exception e) {
            return "[LLM 调用异常: " + e.getMessage() + "]";
        }
    }

    private static String readStream(InputStream is_) throws IOException {
        if (is_ == null) return "";
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        int n;
        while ((n = is_.read(buf)) != -1) baos.write(buf, 0, n);
        return baos.toString(StandardCharsets.UTF_8.name());
    }

    private static String truncate(String s, int max) {
        return s.length() <= max ? s : s.substring(0, max) + "...";
    }

    public static class LlmConfig {
        public String baseUrl = "https://api.siliconflow.cn";
        public String apiKey = "";
        public String model = "deepseek-ai/DeepSeek-R1-0528-Qwen3-8B";
        public String systemPrompt;
        public int maxTokens = 1024;
        public double temperature = 0.7;
    }
}
