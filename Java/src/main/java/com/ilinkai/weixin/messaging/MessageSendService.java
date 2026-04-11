package com.ilinkai.weixin.messaging;

import com.ilinkai.weixin.WeixinApiClient;
import com.ilinkai.weixin.WeixinApiException;
import com.ilinkai.weixin.cdn.CdnException;
import com.ilinkai.weixin.media.MediaUploadService;
import com.ilinkai.weixin.models.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Paths;
import java.util.*;

/** 消息发送服务 */
public class MessageSendService {
    private static final Logger logger = LoggerFactory.getLogger(MessageSendService.class);
    private final WeixinApiClient apiClient;
    private final MediaUploadService uploadService;

    public MessageSendService(WeixinApiClient apiClient, String cdnBaseUrl) {
        this.apiClient = apiClient;
        this.uploadService = new MediaUploadService(apiClient, cdnBaseUrl);
    }

    private static String generateClientId() {
        return "ilinkai-weixin-" + UUID.randomUUID().toString().replace("-", "");
    }

    public String sendText(String toUserId, String text, String contextToken)
            throws WeixinApiException, IOException {
        if (contextToken == null || contextToken.isEmpty())
            throw new IllegalArgumentException("contextToken is required");

        String clientId = generateClientId();
        List<MessageItem> items = new ArrayList<>();
        if (text != null && !text.isEmpty()) {
            MessageItem item = new MessageItem();
            item.setType(Enums.MessageItemType.TEXT);
            MessageItems.TextItem ti = new MessageItems.TextItem();
            ti.setText(text);
            item.setTextItem(ti);
            items.add(item);
        }

        WeixinMessage msg = new WeixinMessage();
        msg.setFromUserId("");
        msg.setToUserId(toUserId);
        msg.setClientId(clientId);
        msg.setMessageType(Enums.MessageType.BOT);
        msg.setMessageState(Enums.MessageState.FINISH);
        msg.setItemList(items);
        msg.setContextToken(contextToken);

        ApiModels.SendMessageRequest request = new ApiModels.SendMessageRequest();
        request.setMessage(msg);
        apiClient.sendMessage(request);
        return clientId;
    }

    public String sendImage(String toUserId, String filePath, String contextToken, String caption)
            throws WeixinApiException, IOException, CdnException {
        if (contextToken == null || contextToken.isEmpty())
            throw new IllegalArgumentException("contextToken is required");

        MediaUploadService.UploadedFileInfo uploaded = uploadService.uploadImage(filePath, toUserId);
        MessageItem imageItem = new MessageItem();
        imageItem.setType(Enums.MessageItemType.IMAGE);
        MessageItems.ImageItem img = new MessageItems.ImageItem();
        CdnMedia media = new CdnMedia();
        media.setEncryptQueryParam(uploaded.downloadEncryptedQueryParam);
        media.setAesKey(Base64.getEncoder().encodeToString(hexStringToBytes(uploaded.aesKey)));
        media.setEncryptType(1);
        img.setMedia(media);
        img.setMidSize(uploaded.fileSizeCiphertext);
        imageItem.setImageItem(img);
        return sendMediaItems(toUserId, caption, imageItem, contextToken, "SendImage");
    }

    public String sendVideo(String toUserId, String filePath, String contextToken, String caption)
            throws WeixinApiException, IOException, CdnException {
        if (contextToken == null || contextToken.isEmpty())
            throw new IllegalArgumentException("contextToken is required");

        MediaUploadService.UploadedFileInfo uploaded = uploadService.uploadVideo(filePath, toUserId);
        MessageItem videoItem = new MessageItem();
        videoItem.setType(Enums.MessageItemType.VIDEO);
        MessageItems.VideoItem vi = new MessageItems.VideoItem();
        CdnMedia media = new CdnMedia();
        media.setEncryptQueryParam(uploaded.downloadEncryptedQueryParam);
        media.setAesKey(Base64.getEncoder().encodeToString(hexStringToBytes(uploaded.aesKey)));
        media.setEncryptType(1);
        vi.setMedia(media);
        vi.setVideoSize(uploaded.fileSizeCiphertext);
        videoItem.setVideoItem(vi);
        return sendMediaItems(toUserId, caption, videoItem, contextToken, "SendVideo");
    }

    public String sendFile(String toUserId, String filePath, String contextToken, String caption)
            throws WeixinApiException, IOException, CdnException {
        if (contextToken == null || contextToken.isEmpty())
            throw new IllegalArgumentException("contextToken is required");

        String fileName = Paths.get(filePath).getFileName().toString();
        MediaUploadService.UploadedFileInfo uploaded = uploadService.uploadFileAttachment(filePath, toUserId);
        MessageItem fileItem = new MessageItem();
        fileItem.setType(Enums.MessageItemType.FILE);
        MessageItems.FileItem fi = new MessageItems.FileItem();
        CdnMedia media = new CdnMedia();
        media.setEncryptQueryParam(uploaded.downloadEncryptedQueryParam);
        media.setAesKey(Base64.getEncoder().encodeToString(hexStringToBytes(uploaded.aesKey)));
        media.setEncryptType(1);
        fi.setMedia(media);
        fi.setFileName(fileName);
        fi.setLength(String.valueOf(uploaded.fileSize));
        fileItem.setFileItem(fi);
        return sendMediaItems(toUserId, caption, fileItem, contextToken, "SendFile");
    }

    private String sendMediaItems(String toUserId, String text, MessageItem mediaItem,
                                   String contextToken, String label) throws WeixinApiException, IOException {
        List<MessageItem> items = new ArrayList<>();
        if (text != null && !text.isEmpty()) {
            MessageItem textItem = new MessageItem();
            textItem.setType(Enums.MessageItemType.TEXT);
            MessageItems.TextItem ti = new MessageItems.TextItem();
            ti.setText(text);
            textItem.setTextItem(ti);
            items.add(textItem);
        }
        items.add(mediaItem);

        String lastClientId = "";
        for (MessageItem item : items) {
            lastClientId = generateClientId();
            WeixinMessage msg = new WeixinMessage();
            msg.setFromUserId("");
            msg.setToUserId(toUserId);
            msg.setClientId(lastClientId);
            msg.setMessageType(Enums.MessageType.BOT);
            msg.setMessageState(Enums.MessageState.FINISH);
            msg.setItemList(Collections.singletonList(item));
            msg.setContextToken(contextToken);

            ApiModels.SendMessageRequest request = new ApiModels.SendMessageRequest();
            request.setMessage(msg);
            apiClient.sendMessage(request);
        }
        return lastClientId;
    }

    public void sendTyping(String ilinkUserId, String typingTicket, boolean isTyping)
            throws WeixinApiException, IOException {
        ApiModels.SendTypingRequest request = new ApiModels.SendTypingRequest();
        request.setILinkUserId(ilinkUserId);
        request.setTypingTicket(typingTicket);
        request.setStatus(isTyping ? Enums.TypingStatus.TYPING : Enums.TypingStatus.CANCEL);
        apiClient.sendTyping(request);
    }

    private static byte[] hexStringToBytes(String hex) {
        byte[] result = new byte[hex.length() / 2];
        for (int i = 0; i < result.length; i++) {
            result[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
        }
        return result;
    }
}
