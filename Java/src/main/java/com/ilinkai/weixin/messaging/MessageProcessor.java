package com.ilinkai.weixin.messaging;

import com.ilinkai.weixin.media.MediaDownloadService;
import com.ilinkai.weixin.models.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;

/** 消息处理服务 */
public class MessageProcessor {
    private static final Logger logger = LoggerFactory.getLogger(MessageProcessor.class);
    private final MediaDownloadService downloadService;

    public MessageProcessor(String cdnBaseUrl) {
        this.downloadService = new MediaDownloadService(cdnBaseUrl);
    }

    public MessageContext convertToContext(WeixinMessage message, String accountId) {
        String fromUserId = message.getFromUserId() != null ? message.getFromUserId() : "";
        String body = extractBodyFromItemList(message.getItemList());

        MessageContext ctx = new MessageContext();
        ctx.setBody(body);
        ctx.setFrom(fromUserId);
        ctx.setTo(fromUserId);
        ctx.setAccountId(accountId);
        ctx.setMessageId("ilinkai-" + UUID.randomUUID().toString().replace("-", ""));
        ctx.setTimestamp(message.getCreateTimeMs());
        ctx.setContextToken(message.getContextToken());
        ctx.setOriginalMessage(message);
        return ctx;
    }

    public void processMedia(MessageContext context, String mediaDir) {
        WeixinMessage message = context.getOriginalMessage();
        if (message == null || message.getItemList() == null || message.getItemList().isEmpty()) return;

        for (MessageItem item : message.getItemList()) {
            if (item.getType() == null) continue;
            try {
                switch (item.getType()) {
                    case Enums.MessageItemType.IMAGE: processImageItem(context, item, mediaDir); break;
                    case Enums.MessageItemType.VOICE: processVoiceItem(context, item, mediaDir); break;
                    case Enums.MessageItemType.FILE: processFileItem(context, item, mediaDir); break;
                    case Enums.MessageItemType.VIDEO: processVideoItem(context, item, mediaDir); break;
                }
                if (context.getMediaPath() != null && !context.getMediaPath().isEmpty()) break;
            } catch (Exception err) {
                logger.error("ProcessMedia: failed to process item type={}", item.getType(), err);
            }
        }
    }

    private void processImageItem(MessageContext ctx, MessageItem item, String mediaDir) throws Exception {
        MessageItems.ImageItem img = item.getImageItem();
        if (img == null || img.getMedia() == null || img.getMedia().getEncryptQueryParam() == null) return;
        String aesKeyBase64 = img.getAesKey() != null && !img.getAesKey().isEmpty()
                ? Base64.getEncoder().encodeToString(hexStringToBytes(img.getAesKey()))
                : img.getMedia().getAesKey();
        byte[] data = downloadService.downloadImage(img.getMedia().getEncryptQueryParam(), aesKeyBase64);
        String ts = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
        ctx.setMediaPath(downloadService.saveMediaToFile(data, mediaDir, "image_" + ts + ".jpg"));
        ctx.setMediaType("image/*");
    }

    private void processVoiceItem(MessageContext ctx, MessageItem item, String mediaDir) throws Exception {
        MessageItems.VoiceItem voice = item.getVoiceItem();
        if (voice == null || voice.getMedia() == null || voice.getMedia().getEncryptQueryParam() == null
                || voice.getMedia().getAesKey() == null) return;
        byte[] data = downloadService.downloadVoice(voice.getMedia().getEncryptQueryParam(), voice.getMedia().getAesKey());
        String ts = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
        ctx.setMediaPath(downloadService.saveMediaToFile(data, mediaDir, "voice_" + ts + ".silk"));
        ctx.setMediaType("audio/silk");
    }

    private void processFileItem(MessageContext ctx, MessageItem item, String mediaDir) throws Exception {
        MessageItems.FileItem fileItem = item.getFileItem();
        if (fileItem == null || fileItem.getMedia() == null || fileItem.getMedia().getEncryptQueryParam() == null
                || fileItem.getMedia().getAesKey() == null) return;
        byte[] data = downloadService.downloadFile(fileItem.getMedia().getEncryptQueryParam(), fileItem.getMedia().getAesKey());
        String fileName = fileItem.getFileName() != null && !fileItem.getFileName().isEmpty()
                ? fileItem.getFileName()
                : "file_" + LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss")) + ".bin";
        ctx.setMediaPath(downloadService.saveMediaToFile(data, mediaDir, fileName));
        ctx.setMediaType(getMimeTypeFromFileName(fileName));
    }

    private void processVideoItem(MessageContext ctx, MessageItem item, String mediaDir) throws Exception {
        MessageItems.VideoItem videoItem = item.getVideoItem();
        if (videoItem == null || videoItem.getMedia() == null || videoItem.getMedia().getEncryptQueryParam() == null
                || videoItem.getMedia().getAesKey() == null) return;
        byte[] data = downloadService.downloadVideo(videoItem.getMedia().getEncryptQueryParam(), videoItem.getMedia().getAesKey());
        String ts = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
        ctx.setMediaPath(downloadService.saveMediaToFile(data, mediaDir, "video_" + ts + ".mp4"));
        ctx.setMediaType("video/mp4");
    }

    public static String extractBodyFromItemList(List<MessageItem> itemList) {
        if (itemList == null || itemList.isEmpty()) return "";
        for (MessageItem item : itemList) {
            if (item.getType() != null && item.getType() == Enums.MessageItemType.TEXT
                    && item.getTextItem() != null && item.getTextItem().getText() != null) {
                String text = item.getTextItem().getText();
                RefMessage refMsg = item.getRefMessage();
                if (refMsg == null) return text;
                if (refMsg.getMessageItem() != null && isMediaItem(refMsg.getMessageItem())) return text;

                List<String> parts = new ArrayList<>();
                if (refMsg.getTitle() != null && !refMsg.getTitle().isEmpty()) parts.add(refMsg.getTitle());
                if (refMsg.getMessageItem() != null) {
                    String refBody = extractBodyFromItemList(Collections.singletonList(refMsg.getMessageItem()));
                    if (!refBody.isEmpty()) parts.add(refBody);
                }
                if (parts.isEmpty()) return text;
                return "[引用: " + String.join(" | ", parts) + "]\n" + text;
            }
            if (item.getType() != null && item.getType() == Enums.MessageItemType.VOICE
                    && item.getVoiceItem() != null && item.getVoiceItem().getText() != null) {
                return item.getVoiceItem().getText();
            }
        }
        return "";
    }

    public static boolean isMediaItem(MessageItem item) {
        if (item.getType() == null) return false;
        int t = item.getType();
        return t == Enums.MessageItemType.IMAGE || t == Enums.MessageItemType.VIDEO
                || t == Enums.MessageItemType.FILE || t == Enums.MessageItemType.VOICE;
    }

    private static String getMimeTypeFromFileName(String fileName) {
        String ext = "";
        int dot = fileName.lastIndexOf('.');
        if (dot >= 0) ext = fileName.substring(dot).toLowerCase();
        switch (ext) {
            case ".jpg": case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            case ".gif": return "image/gif";
            case ".pdf": return "application/pdf";
            case ".doc": return "application/msword";
            case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case ".xls": return "application/vnd.ms-excel";
            case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case ".zip": return "application/zip";
            case ".mp3": return "audio/mpeg";
            case ".mp4": return "video/mp4";
            default: return "application/octet-stream";
        }
    }

    private static byte[] hexStringToBytes(String hex) {
        byte[] result = new byte[hex.length() / 2];
        for (int i = 0; i < result.length; i++) {
            result[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
        }
        return result;
    }
}
