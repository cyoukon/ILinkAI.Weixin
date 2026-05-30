package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/** API请求/响应模型集合 */
public class ApiModels {

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetUploadUrlRequest {
        @JsonProperty("filekey") private String fileKey;
        @JsonProperty("media_type") private Integer mediaType;
        @JsonProperty("to_user_id") private String toUserId;
        @JsonProperty("rawsize") private Integer rawSize;
        @JsonProperty("rawfilemd5") private String rawFileMd5;
        @JsonProperty("filesize") private Integer fileSize;
        @JsonProperty("thumb_rawsize") private Integer thumbRawSize;
        @JsonProperty("thumb_rawfilemd5") private String thumbRawFileMd5;
        @JsonProperty("thumb_filesize") private Integer thumbFileSize;
        @JsonProperty("no_need_thumb") private Boolean noNeedThumb;
        @JsonProperty("aeskey") private String aesKey;
        @JsonProperty("base_info") private BaseInfo baseInfo;

        public String getFileKey() { return fileKey; }
        public void setFileKey(String v) { this.fileKey = v; }
        public Integer getMediaType() { return mediaType; }
        public void setMediaType(Integer v) { this.mediaType = v; }
        public String getToUserId() { return toUserId; }
        public void setToUserId(String v) { this.toUserId = v; }
        public Integer getRawSize() { return rawSize; }
        public void setRawSize(Integer v) { this.rawSize = v; }
        public String getRawFileMd5() { return rawFileMd5; }
        public void setRawFileMd5(String v) { this.rawFileMd5 = v; }
        public Integer getFileSize() { return fileSize; }
        public void setFileSize(Integer v) { this.fileSize = v; }
        public Integer getThumbRawSize() { return thumbRawSize; }
        public void setThumbRawSize(Integer v) { this.thumbRawSize = v; }
        public String getThumbRawFileMd5() { return thumbRawFileMd5; }
        public void setThumbRawFileMd5(String v) { this.thumbRawFileMd5 = v; }
        public Integer getThumbFileSize() { return thumbFileSize; }
        public void setThumbFileSize(Integer v) { this.thumbFileSize = v; }
        public Boolean getNoNeedThumb() { return noNeedThumb; }
        public void setNoNeedThumb(Boolean v) { this.noNeedThumb = v; }
        public String getAesKey() { return aesKey; }
        public void setAesKey(String v) { this.aesKey = v; }
        public BaseInfo getBaseInfo() { return baseInfo; }
        public void setBaseInfo(BaseInfo v) { this.baseInfo = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetUploadUrlResponse {
        @JsonProperty("upload_param") private String uploadParam;
        @JsonProperty("upload_full_url") private String uploadFullUrl;
        @JsonProperty("thumb_upload_param") private String thumbUploadParam;
        @JsonProperty("thumb_upload_full_url") private String thumbUploadFullUrl;
        public String getUploadParam() { return uploadParam; }
        public void setUploadParam(String v) { this.uploadParam = v; }
        public String getUploadFullUrl() { return uploadFullUrl; }
        public void setUploadFullUrl(String v) { this.uploadFullUrl = v; }
        public String getThumbUploadParam() { return thumbUploadParam; }
        public void setThumbUploadParam(String v) { this.thumbUploadParam = v; }
        public String getThumbUploadFullUrl() { return thumbUploadFullUrl; }
        public void setThumbUploadFullUrl(String v) { this.thumbUploadFullUrl = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetUpdatesRequest {
        @JsonProperty("get_updates_buf") private String getUpdatesBuf;
        @JsonProperty("base_info") private BaseInfo baseInfo;
        public String getGetUpdatesBuf() { return getUpdatesBuf; }
        public void setGetUpdatesBuf(String v) { this.getUpdatesBuf = v; }
        public BaseInfo getBaseInfo() { return baseInfo; }
        public void setBaseInfo(BaseInfo v) { this.baseInfo = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetUpdatesResponse {
        @JsonProperty("ret") private Integer ret;
        @JsonProperty("errcode") private Integer errCode;
        @JsonProperty("errmsg") private String errMsg;
        @JsonProperty("msgs") private List<WeixinMessage> messages;
        @JsonProperty("get_updates_buf") private String getUpdatesBuf;
        @JsonProperty("longpolling_timeout_ms") private Integer longPollingTimeoutMs;

        public Integer getRet() { return ret; }
        public void setRet(Integer v) { this.ret = v; }
        public Integer getErrCode() { return errCode; }
        public void setErrCode(Integer v) { this.errCode = v; }
        public String getErrMsg() { return errMsg; }
        public void setErrMsg(String v) { this.errMsg = v; }
        public List<WeixinMessage> getMessages() { return messages; }
        public void setMessages(List<WeixinMessage> v) { this.messages = v; }
        public String getGetUpdatesBuf() { return getUpdatesBuf; }
        public void setGetUpdatesBuf(String v) { this.getUpdatesBuf = v; }
        public Integer getLongPollingTimeoutMs() { return longPollingTimeoutMs; }
        public void setLongPollingTimeoutMs(Integer v) { this.longPollingTimeoutMs = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class SendMessageRequest {
        @JsonProperty("msg") private WeixinMessage message;
        public WeixinMessage getMessage() { return message; }
        public void setMessage(WeixinMessage v) { this.message = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class SendTypingRequest {
        @JsonProperty("ilink_user_id") private String iLinkUserId;
        @JsonProperty("typing_ticket") private String typingTicket;
        @JsonProperty("status") private Integer status;
        @JsonProperty("base_info") private BaseInfo baseInfo;

        public String getILinkUserId() { return iLinkUserId; }
        public void setILinkUserId(String v) { this.iLinkUserId = v; }
        public String getTypingTicket() { return typingTicket; }
        public void setTypingTicket(String v) { this.typingTicket = v; }
        public Integer getStatus() { return status; }
        public void setStatus(Integer v) { this.status = v; }
        public BaseInfo getBaseInfo() { return baseInfo; }
        public void setBaseInfo(BaseInfo v) { this.baseInfo = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class SendTypingResponse {
        @JsonProperty("ret") private Integer ret;
        @JsonProperty("errmsg") private String errMsg;
        public Integer getRet() { return ret; }
        public void setRet(Integer v) { this.ret = v; }
        public String getErrMsg() { return errMsg; }
        public void setErrMsg(String v) { this.errMsg = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetConfigRequest {
        @JsonProperty("ilink_user_id") private String iLinkUserId;
        @JsonProperty("context_token") private String contextToken;
        @JsonProperty("base_info") private BaseInfo baseInfo;

        public String getILinkUserId() { return iLinkUserId; }
        public void setILinkUserId(String v) { this.iLinkUserId = v; }
        public String getContextToken() { return contextToken; }
        public void setContextToken(String v) { this.contextToken = v; }
        public BaseInfo getBaseInfo() { return baseInfo; }
        public void setBaseInfo(BaseInfo v) { this.baseInfo = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetConfigResponse {
        @JsonProperty("ret") private Integer ret;
        @JsonProperty("errmsg") private String errMsg;
        @JsonProperty("typing_ticket") private String typingTicket;
        public Integer getRet() { return ret; }
        public void setRet(Integer v) { this.ret = v; }
        public String getErrMsg() { return errMsg; }
        public void setErrMsg(String v) { this.errMsg = v; }
        public String getTypingTicket() { return typingTicket; }
        public void setTypingTicket(String v) { this.typingTicket = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetQRCodeResponse {
        @JsonProperty("qrcode") private String qrCode;
        @JsonProperty("qrcode_img_content") private String qrCodeImgContent;
        public String getQrCode() { return qrCode; }
        public void setQrCode(String v) { this.qrCode = v; }
        public String getQrCodeImgContent() { return qrCodeImgContent; }
        public void setQrCodeImgContent(String v) { this.qrCodeImgContent = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class GetQRCodeStatusResponse {
        @JsonProperty("status") private String status;
        @JsonProperty("bot_token") private String botToken;
        @JsonProperty("ilink_bot_id") private String iLinkBotId;
        @JsonProperty("baseurl") private String baseUrl;
        @JsonProperty("ilink_user_id") private String iLinkUserId;

        public String getStatus() { return status; }
        public void setStatus(String v) { this.status = v; }
        public String getBotToken() { return botToken; }
        public void setBotToken(String v) { this.botToken = v; }
        public String getILinkBotId() { return iLinkBotId; }
        public void setILinkBotId(String v) { this.iLinkBotId = v; }
        public String getBaseUrl() { return baseUrl; }
        public void setBaseUrl(String v) { this.baseUrl = v; }
        public String getILinkUserId() { return iLinkUserId; }
        public void setILinkUserId(String v) { this.iLinkUserId = v; }
    }
}
