package com.ilinkai.weixin.auth;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ilinkai.weixin.WeixinApiClient;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

/** 账户存储服务 */
public class AccountStore {
    private static final Logger logger = LoggerFactory.getLogger(AccountStore.class);
    private static final ObjectMapper MAPPER = new ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
            .setSerializationInclusion(JsonInclude.Include.NON_NULL);

    private final String stateDir;

    public AccountStore() { this(null); }

    public AccountStore(String stateDir) {
        this.stateDir = stateDir != null ? stateDir : getDefaultStateDir();
    }

    private static String getDefaultStateDir() {
        String envPath = System.getenv("ILINKAI_STATE_DIR");
        if (envPath != null && !envPath.trim().isEmpty()) return envPath;
        return System.getProperty("user.home") + File.separator + ".ilinkai";
    }

    private String getWeixinStateDir() { return stateDir + File.separator + "weixin"; }
    private String getAccountIndexPath() { return getWeixinStateDir() + File.separator + "accounts.json"; }
    private String getAccountsDir() { return getWeixinStateDir() + File.separator + "accounts"; }
    private String getAccountFilePath(String accountId) {
        return getAccountsDir() + File.separator + sanitizeAccountId(accountId) + ".json";
    }

    private static String sanitizeAccountId(String accountId) {
        String safe = accountId.trim().toLowerCase();
        for (char c : new char[]{'/', '\\', ':', '*', '?', '"', '<', '>', '|'}) {
            safe = safe.replace(c, '_');
        }
        return safe.replace("..", "_");
    }

    public static String normalizeAccountId(String rawId) {
        String trimmed = rawId.trim().toLowerCase();
        if (trimmed.endsWith("@im.bot")) return trimmed.replace("@im.bot", "-im-bot");
        if (trimmed.endsWith("@im.wechat")) return trimmed.replace("@im.wechat", "-im-wechat");
        return trimmed;
    }

    public static String deriveRawAccountId(String normalizedId) {
        if (normalizedId.endsWith("-im-bot"))
            return normalizedId.substring(0, normalizedId.length() - 7) + "@im.bot";
        if (normalizedId.endsWith("-im-wechat"))
            return normalizedId.substring(0, normalizedId.length() - 10) + "@im.wechat";
        return null;
    }

    @SuppressWarnings("unchecked")
    public List<String> listAccountIds() {
        File file = new File(getAccountIndexPath());
        try {
            if (!file.exists()) return new ArrayList<>();
            List<String> parsed = MAPPER.readValue(file, List.class);
            if (parsed == null) return new ArrayList<>();
            return parsed.stream().filter(id -> id != null && !id.trim().isEmpty()).collect(Collectors.toList());
        } catch (Exception e) {
            return new ArrayList<>();
        }
    }

    public void registerAccountId(String accountId) throws IOException {
        new File(getWeixinStateDir()).mkdirs();
        List<String> existing = listAccountIds();
        if (existing.contains(accountId)) return;
        existing.add(accountId);
        MAPPER.writerWithDefaultPrettyPrinter().writeValue(new File(getAccountIndexPath()), existing);
    }

    public WeixinAccountData loadAccount(String accountId) {
        File file = new File(getAccountFilePath(accountId));
        if (file.exists()) return readAccountFile(file);
        String rawId = deriveRawAccountId(accountId);
        if (rawId != null) {
            File compatFile = new File(getAccountFilePath(rawId));
            if (compatFile.exists()) return readAccountFile(compatFile);
        }
        return null;
    }

    private WeixinAccountData readAccountFile(File file) {
        try {
            return MAPPER.readValue(file, WeixinAccountData.class);
        } catch (Exception e) {
            return null;
        }
    }

    public void saveAccount(String accountId, WeixinAccountData data) throws IOException {
        new File(getAccountsDir()).mkdirs();
        WeixinAccountData existing = loadAccount(accountId);
        if (existing == null) existing = new WeixinAccountData();

        WeixinAccountData merged = new WeixinAccountData();
        merged.setToken(data.getToken() != null ? data.getToken().trim() : existing.getToken());
        merged.setBaseUrl(data.getBaseUrl() != null ? data.getBaseUrl().trim() : existing.getBaseUrl());
        merged.setUserId(data.getUserId() != null ? data.getUserId() : existing.getUserId());
        merged.setSavedAt(Instant.now().toString());

        MAPPER.writerWithDefaultPrettyPrinter().writeValue(new File(getAccountFilePath(accountId)), merged);
    }

    public void clearAccount(String accountId) {
        try {
            File file = new File(getAccountFilePath(accountId));
            if (file.exists()) file.delete();
        } catch (Exception ignored) {}
    }

    public ResolvedWeixinAccount resolveAccount(String accountId) {
        if (accountId == null || accountId.trim().isEmpty()) {
            throw new IllegalArgumentException("accountId is required");
        }
        String id = normalizeAccountId(accountId);
        WeixinAccountData accountData = loadAccount(id);

        ResolvedWeixinAccount resolved = new ResolvedWeixinAccount();
        resolved.setAccountId(id);
        resolved.setBaseUrl(accountData != null && accountData.getBaseUrl() != null
                ? accountData.getBaseUrl().trim() : WeixinApiClient.DEFAULT_BASE_URL);
        resolved.setCdnBaseUrl(WeixinApiClient.DEFAULT_CDN_BASE_URL);
        resolved.setToken(accountData != null ? (accountData.getToken() != null ? accountData.getToken().trim() : null) : null);
        resolved.setEnabled(true);
        resolved.setConfigured(accountData != null && accountData.getToken() != null && !accountData.getToken().trim().isEmpty());
        return resolved;
    }

    /** 微信账户数据 */
    public static class WeixinAccountData {
        @JsonProperty("token") private String token;
        @JsonProperty("saved_at") private String savedAt;
        @JsonProperty("base_url") private String baseUrl;
        @JsonProperty("user_id") private String userId;

        public String getToken() { return token; }
        public void setToken(String v) { this.token = v; }
        public String getSavedAt() { return savedAt; }
        public void setSavedAt(String v) { this.savedAt = v; }
        public String getBaseUrl() { return baseUrl; }
        public void setBaseUrl(String v) { this.baseUrl = v; }
        public String getUserId() { return userId; }
        public void setUserId(String v) { this.userId = v; }
    }

    /** 已解析的微信账户信息 */
    public static class ResolvedWeixinAccount {
        private String accountId = "";
        private String baseUrl = WeixinApiClient.DEFAULT_BASE_URL;
        private String cdnBaseUrl = WeixinApiClient.DEFAULT_CDN_BASE_URL;
        private String token;
        private boolean enabled = true;
        private boolean configured;
        private String name;

        public String getAccountId() { return accountId; }
        public void setAccountId(String v) { this.accountId = v; }
        public String getBaseUrl() { return baseUrl; }
        public void setBaseUrl(String v) { this.baseUrl = v; }
        public String getCdnBaseUrl() { return cdnBaseUrl; }
        public void setCdnBaseUrl(String v) { this.cdnBaseUrl = v; }
        public String getToken() { return token; }
        public void setToken(String v) { this.token = v; }
        public boolean isEnabled() { return enabled; }
        public void setEnabled(boolean v) { this.enabled = v; }
        public boolean isConfigured() { return configured; }
        public void setConfigured(boolean v) { this.configured = v; }
        public String getName() { return name; }
        public void setName(String v) { this.name = v; }
    }
}
