package com.ilinkai.weixin.messaging;

import com.ilinkai.weixin.models.WeixinMessage;

/** 消息上下文信息 */
public class MessageContext {
    private String body = "";
    private String from = "";
    private String to = "";
    private String accountId = "";
    private String messageId = "";
    private Long timestamp;
    private String contextToken;
    private String mediaPath;
    private String mediaType;
    private WeixinMessage originalMessage;

    public String getBody() { return body; }
    public void setBody(String v) { this.body = v; }
    public String getFrom() { return from; }
    public void setFrom(String v) { this.from = v; }
    public String getTo() { return to; }
    public void setTo(String v) { this.to = v; }
    public String getAccountId() { return accountId; }
    public void setAccountId(String v) { this.accountId = v; }
    public String getMessageId() { return messageId; }
    public void setMessageId(String v) { this.messageId = v; }
    public Long getTimestamp() { return timestamp; }
    public void setTimestamp(Long v) { this.timestamp = v; }
    public String getContextToken() { return contextToken; }
    public void setContextToken(String v) { this.contextToken = v; }
    public String getMediaPath() { return mediaPath; }
    public void setMediaPath(String v) { this.mediaPath = v; }
    public String getMediaType() { return mediaType; }
    public void setMediaType(String v) { this.mediaType = v; }
    public WeixinMessage getOriginalMessage() { return originalMessage; }
    public void setOriginalMessage(WeixinMessage v) { this.originalMessage = v; }
}
