package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

/** 消息项子类型集合 */
public class MessageItems {

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class TextItem {
        @JsonProperty("text")
        private String text;
        public String getText() { return text; }
        public void setText(String v) { this.text = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class ImageItem {
        @JsonProperty("media")
        private CdnMedia media;
        @JsonProperty("thumb_media")
        private CdnMedia thumbMedia;
        @JsonProperty("aeskey")
        private String aesKey;
        @JsonProperty("url")
        private String url;
        @JsonProperty("mid_size")
        private Integer midSize;
        @JsonProperty("thumb_size")
        private Integer thumbSize;
        @JsonProperty("thumb_height")
        private Integer thumbHeight;
        @JsonProperty("thumb_width")
        private Integer thumbWidth;
        @JsonProperty("hd_size")
        private Integer hdSize;

        public CdnMedia getMedia() { return media; }
        public void setMedia(CdnMedia v) { this.media = v; }
        public CdnMedia getThumbMedia() { return thumbMedia; }
        public void setThumbMedia(CdnMedia v) { this.thumbMedia = v; }
        public String getAesKey() { return aesKey; }
        public void setAesKey(String v) { this.aesKey = v; }
        public String getUrl() { return url; }
        public void setUrl(String v) { this.url = v; }
        public Integer getMidSize() { return midSize; }
        public void setMidSize(Integer v) { this.midSize = v; }
        public Integer getThumbSize() { return thumbSize; }
        public void setThumbSize(Integer v) { this.thumbSize = v; }
        public Integer getThumbHeight() { return thumbHeight; }
        public void setThumbHeight(Integer v) { this.thumbHeight = v; }
        public Integer getThumbWidth() { return thumbWidth; }
        public void setThumbWidth(Integer v) { this.thumbWidth = v; }
        public Integer getHdSize() { return hdSize; }
        public void setHdSize(Integer v) { this.hdSize = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class VoiceItem {
        @JsonProperty("media")
        private CdnMedia media;
        @JsonProperty("encode_type")
        private Integer encodeType;
        @JsonProperty("bits_per_sample")
        private Integer bitsPerSample;
        @JsonProperty("sample_rate")
        private Integer sampleRate;
        @JsonProperty("playtime")
        private Integer playTime;
        @JsonProperty("text")
        private String text;

        public CdnMedia getMedia() { return media; }
        public void setMedia(CdnMedia v) { this.media = v; }
        public Integer getEncodeType() { return encodeType; }
        public void setEncodeType(Integer v) { this.encodeType = v; }
        public Integer getBitsPerSample() { return bitsPerSample; }
        public void setBitsPerSample(Integer v) { this.bitsPerSample = v; }
        public Integer getSampleRate() { return sampleRate; }
        public void setSampleRate(Integer v) { this.sampleRate = v; }
        public Integer getPlayTime() { return playTime; }
        public void setPlayTime(Integer v) { this.playTime = v; }
        public String getText() { return text; }
        public void setText(String v) { this.text = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class FileItem {
        @JsonProperty("media")
        private CdnMedia media;
        @JsonProperty("file_name")
        private String fileName;
        @JsonProperty("md5")
        private String md5;
        @JsonProperty("len")
        private String length;

        public CdnMedia getMedia() { return media; }
        public void setMedia(CdnMedia v) { this.media = v; }
        public String getFileName() { return fileName; }
        public void setFileName(String v) { this.fileName = v; }
        public String getMd5() { return md5; }
        public void setMd5(String v) { this.md5 = v; }
        public String getLength() { return length; }
        public void setLength(String v) { this.length = v; }
    }

    @JsonInclude(JsonInclude.Include.NON_NULL)
    public static class VideoItem {
        @JsonProperty("media")
        private CdnMedia media;
        @JsonProperty("video_size")
        private Integer videoSize;
        @JsonProperty("play_length")
        private Integer playLength;
        @JsonProperty("video_md5")
        private String videoMd5;
        @JsonProperty("thumb_media")
        private CdnMedia thumbMedia;
        @JsonProperty("thumb_size")
        private Integer thumbSize;
        @JsonProperty("thumb_height")
        private Integer thumbHeight;
        @JsonProperty("thumb_width")
        private Integer thumbWidth;

        public CdnMedia getMedia() { return media; }
        public void setMedia(CdnMedia v) { this.media = v; }
        public Integer getVideoSize() { return videoSize; }
        public void setVideoSize(Integer v) { this.videoSize = v; }
        public Integer getPlayLength() { return playLength; }
        public void setPlayLength(Integer v) { this.playLength = v; }
        public String getVideoMd5() { return videoMd5; }
        public void setVideoMd5(String v) { this.videoMd5 = v; }
        public CdnMedia getThumbMedia() { return thumbMedia; }
        public void setThumbMedia(CdnMedia v) { this.thumbMedia = v; }
        public Integer getThumbSize() { return thumbSize; }
        public void setThumbSize(Integer v) { this.thumbSize = v; }
        public Integer getThumbHeight() { return thumbHeight; }
        public void setThumbHeight(Integer v) { this.thumbHeight = v; }
        public Integer getThumbWidth() { return thumbWidth; }
        public void setThumbWidth(Integer v) { this.thumbWidth = v; }
    }
}
