package com.ilinkai.weixin;

/** ILinkai微信API客户端配置选项 */
public class WeixinApiClientOptions {
    private String baseUrl = "https://ilinkai.weixin.qq.com";
    private String cdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";
    private String token;
    private int timeoutMs = 15000;
    private int longPollTimeoutMs = 35000;
    private int configTimeoutMs = 10000;
    private String routeTag;

    public String getBaseUrl() { return baseUrl; }
    public void setBaseUrl(String v) { this.baseUrl = v; }
    public String getCdnBaseUrl() { return cdnBaseUrl; }
    public void setCdnBaseUrl(String v) { this.cdnBaseUrl = v; }
    public String getToken() { return token; }
    public void setToken(String v) { this.token = v; }
    public int getTimeoutMs() { return timeoutMs; }
    public void setTimeoutMs(int v) { this.timeoutMs = v; }
    public int getLongPollTimeoutMs() { return longPollTimeoutMs; }
    public void setLongPollTimeoutMs(int v) { this.longPollTimeoutMs = v; }
    public int getConfigTimeoutMs() { return configTimeoutMs; }
    public void setConfigTimeoutMs(int v) { this.configTimeoutMs = v; }
    public String getRouteTag() { return routeTag; }
    public void setRouteTag(String v) { this.routeTag = v; }
}
