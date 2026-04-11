package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/** 微信消息（统一消息格式） */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class WeixinMessage {
    @JsonProperty("seq")
    private Long seq;
    @JsonProperty("message_id")
    private Long messageId;
    @JsonProperty("from_user_id")
    private String fromUserId;
    @JsonProperty("to_user_id")
    private String toUserId;
    @JsonProperty("client_id")
    private String clientId;
    @JsonProperty("create_time_ms")
    private Long createTimeMs;
    @JsonProperty("update_time_ms")
    private Long updateTimeMs;
    @JsonProperty("delete_time_ms")
    private Long deleteTimeMs;
    @JsonProperty("session_id")
    private String sessionId;
    @JsonProperty("group_id")
    private String groupId;
    @JsonProperty("message_type")
    private Integer messageType;
    @JsonProperty("message_state")
    private Integer messageState;
    @JsonProperty("item_list")
    private List<MessageItem> itemList;
    @JsonProperty("context_token")
    private String contextToken;

    public Long getSeq() { return seq; }
    public void setSeq(Long v) { this.seq = v; }
    public Long getMessageId() { return messageId; }
    public void setMessageId(Long v) { this.messageId = v; }
    public String getFromUserId() { return fromUserId; }
    public void setFromUserId(String v) { this.fromUserId = v; }
    public String getToUserId() { return toUserId; }
    public void setToUserId(String v) { this.toUserId = v; }
    public String getClientId() { return clientId; }
    public void setClientId(String v) { this.clientId = v; }
    public Long getCreateTimeMs() { return createTimeMs; }
    public void setCreateTimeMs(Long v) { this.createTimeMs = v; }
    public Long getUpdateTimeMs() { return updateTimeMs; }
    public void setUpdateTimeMs(Long v) { this.updateTimeMs = v; }
    public Long getDeleteTimeMs() { return deleteTimeMs; }
    public void setDeleteTimeMs(Long v) { this.deleteTimeMs = v; }
    public String getSessionId() { return sessionId; }
    public void setSessionId(String v) { this.sessionId = v; }
    public String getGroupId() { return groupId; }
    public void setGroupId(String v) { this.groupId = v; }
    public Integer getMessageType() { return messageType; }
    public void setMessageType(Integer v) { this.messageType = v; }
    public Integer getMessageState() { return messageState; }
    public void setMessageState(Integer v) { this.messageState = v; }
    public List<MessageItem> getItemList() { return itemList; }
    public void setItemList(List<MessageItem> v) { this.itemList = v; }
    public String getContextToken() { return contextToken; }
    public void setContextToken(String v) { this.contextToken = v; }
}
