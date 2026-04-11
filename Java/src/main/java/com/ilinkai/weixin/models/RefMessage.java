package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

/** 引用消息 */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class RefMessage {
    @JsonProperty("message_item")
    private MessageItem messageItem;

    @JsonProperty("title")
    private String title;

    public MessageItem getMessageItem() { return messageItem; }
    public void setMessageItem(MessageItem v) { this.messageItem = v; }
    public String getTitle() { return title; }
    public void setTitle(String v) { this.title = v; }
}
