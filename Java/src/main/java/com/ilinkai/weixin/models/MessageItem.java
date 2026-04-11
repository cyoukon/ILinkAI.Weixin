package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

/** 消息项 */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class MessageItem {
    @JsonProperty("type")
    private Integer type;
    @JsonProperty("create_time_ms")
    private Long createTimeMs;
    @JsonProperty("update_time_ms")
    private Long updateTimeMs;
    @JsonProperty("is_completed")
    private Boolean isCompleted;
    @JsonProperty("msg_id")
    private String msgId;
    @JsonProperty("ref_msg")
    private RefMessage refMessage;
    @JsonProperty("text_item")
    private MessageItems.TextItem textItem;
    @JsonProperty("image_item")
    private MessageItems.ImageItem imageItem;
    @JsonProperty("voice_item")
    private MessageItems.VoiceItem voiceItem;
    @JsonProperty("file_item")
    private MessageItems.FileItem fileItem;
    @JsonProperty("video_item")
    private MessageItems.VideoItem videoItem;

    public Integer getType() { return type; }
    public void setType(Integer v) { this.type = v; }
    public Long getCreateTimeMs() { return createTimeMs; }
    public void setCreateTimeMs(Long v) { this.createTimeMs = v; }
    public Long getUpdateTimeMs() { return updateTimeMs; }
    public void setUpdateTimeMs(Long v) { this.updateTimeMs = v; }
    public Boolean getIsCompleted() { return isCompleted; }
    public void setIsCompleted(Boolean v) { this.isCompleted = v; }
    public String getMsgId() { return msgId; }
    public void setMsgId(String v) { this.msgId = v; }
    public RefMessage getRefMessage() { return refMessage; }
    public void setRefMessage(RefMessage v) { this.refMessage = v; }
    public MessageItems.TextItem getTextItem() { return textItem; }
    public void setTextItem(MessageItems.TextItem v) { this.textItem = v; }
    public MessageItems.ImageItem getImageItem() { return imageItem; }
    public void setImageItem(MessageItems.ImageItem v) { this.imageItem = v; }
    public MessageItems.VoiceItem getVoiceItem() { return voiceItem; }
    public void setVoiceItem(MessageItems.VoiceItem v) { this.voiceItem = v; }
    public MessageItems.FileItem getFileItem() { return fileItem; }
    public void setFileItem(MessageItems.FileItem v) { this.fileItem = v; }
    public MessageItems.VideoItem getVideoItem() { return videoItem; }
    public void setVideoItem(MessageItems.VideoItem v) { this.videoItem = v; }
}
