package com.ilinkai.weixin.models;

/** 上传媒体类型枚举 */
public class Enums {

    public static final class UploadMediaType {
        public static final int IMAGE = 1;
        public static final int VIDEO = 2;
        public static final int FILE = 3;
        public static final int VOICE = 4;
    }

    public static final class MessageType {
        public static final int NONE = 0;
        public static final int USER = 1;
        public static final int BOT = 2;
    }

    public static final class MessageItemType {
        public static final int NONE = 0;
        public static final int TEXT = 1;
        public static final int IMAGE = 2;
        public static final int VOICE = 3;
        public static final int FILE = 4;
        public static final int VIDEO = 5;
    }

    public static final class MessageState {
        public static final int NEW = 0;
        public static final int GENERATING = 1;
        public static final int FINISH = 2;
    }

    public static final class TypingStatus {
        public static final int TYPING = 1;
        public static final int CANCEL = 2;
    }

    public static final class VoiceEncodeType {
        public static final int PCM = 1;
        public static final int ADPCM = 2;
        public static final int FEATURE = 3;
        public static final int SPEEX = 4;
        public static final int AMR = 5;
        public static final int SILK = 6;
        public static final int MP3 = 7;
        public static final int OGG_SPEEX = 8;
    }

    public static final class QRCodeStatus {
        public static final String WAIT = "wait";
        public static final String SCANNED = "scaned";
        public static final String CONFIRMED = "confirmed";
        public static final String EXPIRED = "expired";
    }
}
