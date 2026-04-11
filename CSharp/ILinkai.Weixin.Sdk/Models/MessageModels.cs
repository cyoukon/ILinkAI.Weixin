using System.Text.Json.Serialization;

namespace ILinkai.Weixin.Sdk.Models;

/// <summary>
/// 基础信息，附加到每个CGI请求
/// </summary>
public class BaseInfo
{
    /// <summary>
    /// 渠道版本号
    /// </summary>
    [JsonPropertyName("channel_version")]
    public string? ChannelVersion { get; set; }
}

/// <summary>
/// CDN媒体引用信息
/// </summary>
public class CdnMedia
{
    /// <summary>
    /// 加密的查询参数
    /// </summary>
    [JsonPropertyName("encrypt_query_param")]
    public string? EncryptQueryParam { get; set; }

    /// <summary>
    /// AES密钥（Base64编码）
    /// </summary>
    [JsonPropertyName("aes_key")]
    public string? AesKey { get; set; }

    /// <summary>
    /// 加密类型: 0=只加密fileid, 1=打包缩略图/中图等信息
    /// </summary>
    [JsonPropertyName("encrypt_type")]
    public int? EncryptType { get; set; }
}

/// <summary>
/// 文本消息项
/// </summary>
public class TextItem
{
    /// <summary>
    /// 文本内容
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// 图片消息项
/// </summary>
public class ImageItem
{
    /// <summary>
    /// 原图CDN引用
    /// </summary>
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    /// <summary>
    /// 缩略图CDN引用
    /// </summary>
    [JsonPropertyName("thumb_media")]
    public CdnMedia? ThumbMedia { get; set; }

    /// <summary>
    /// AES-128密钥（十六进制字符串，16字节）
    /// </summary>
    [JsonPropertyName("aeskey")]
    public string? AesKey { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// 中图大小
    /// </summary>
    [JsonPropertyName("mid_size")]
    public int? MidSize { get; set; }

    /// <summary>
    /// 缩略图大小
    /// </summary>
    [JsonPropertyName("thumb_size")]
    public int? ThumbSize { get; set; }

    /// <summary>
    /// 缩略图高度
    /// </summary>
    [JsonPropertyName("thumb_height")]
    public int? ThumbHeight { get; set; }

    /// <summary>
    /// 缩略图宽度
    /// </summary>
    [JsonPropertyName("thumb_width")]
    public int? ThumbWidth { get; set; }

    /// <summary>
    /// 高清图大小
    /// </summary>
    [JsonPropertyName("hd_size")]
    public int? HdSize { get; set; }
}

/// <summary>
/// 语音消息项
/// </summary>
public class VoiceItem
{
    /// <summary>
    /// CDN媒体引用
    /// </summary>
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    /// <summary>
    /// 语音编码类型
    /// </summary>
    [JsonPropertyName("encode_type")]
    public int? EncodeType { get; set; }

    /// <summary>
    /// 每样本位数
    /// </summary>
    [JsonPropertyName("bits_per_sample")]
    public int? BitsPerSample { get; set; }

    /// <summary>
    /// 采样率(Hz)
    /// </summary>
    [JsonPropertyName("sample_rate")]
    public int? SampleRate { get; set; }

    /// <summary>
    /// 语音长度(毫秒)
    /// </summary>
    [JsonPropertyName("playtime")]
    public int? PlayTime { get; set; }

    /// <summary>
    /// 语音转文字内容
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// 文件消息项
/// </summary>
public class FileItem
{
    /// <summary>
    /// CDN媒体引用
    /// </summary>
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// 文件MD5
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// 文件长度
    /// </summary>
    [JsonPropertyName("len")]
    public string? Length { get; set; }
}

/// <summary>
/// 视频消息项
/// </summary>
public class VideoItem
{
    /// <summary>
    /// CDN媒体引用
    /// </summary>
    [JsonPropertyName("media")]
    public CdnMedia? Media { get; set; }

    /// <summary>
    /// 视频大小
    /// </summary>
    [JsonPropertyName("video_size")]
    public int? VideoSize { get; set; }

    /// <summary>
    /// 播放时长
    /// </summary>
    [JsonPropertyName("play_length")]
    public int? PlayLength { get; set; }

    /// <summary>
    /// 视频MD5
    /// </summary>
    [JsonPropertyName("video_md5")]
    public string? VideoMd5 { get; set; }

    /// <summary>
    /// 缩略图CDN引用
    /// </summary>
    [JsonPropertyName("thumb_media")]
    public CdnMedia? ThumbMedia { get; set; }

    /// <summary>
    /// 缩略图大小
    /// </summary>
    [JsonPropertyName("thumb_size")]
    public int? ThumbSize { get; set; }

    /// <summary>
    /// 缩略图高度
    /// </summary>
    [JsonPropertyName("thumb_height")]
    public int? ThumbHeight { get; set; }

    /// <summary>
    /// 缩略图宽度
    /// </summary>
    [JsonPropertyName("thumb_width")]
    public int? ThumbWidth { get; set; }
}

/// <summary>
/// 引用消息
/// </summary>
public class RefMessage
{
    /// <summary>
    /// 引用的消息项
    /// </summary>
    [JsonPropertyName("message_item")]
    public MessageItem? MessageItem { get; set; }

    /// <summary>
    /// 摘要标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary>
/// 消息项
/// </summary>
public class MessageItem
{
    /// <summary>
    /// 消息项类型
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// 创建时间(毫秒)
    /// </summary>
    [JsonPropertyName("create_time_ms")]
    public long? CreateTimeMs { get; set; }

    /// <summary>
    /// 更新时间(毫秒)
    /// </summary>
    [JsonPropertyName("update_time_ms")]
    public long? UpdateTimeMs { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool? IsCompleted { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }

    /// <summary>
    /// 引用消息
    /// </summary>
    [JsonPropertyName("ref_msg")]
    public RefMessage? RefMessage { get; set; }

    /// <summary>
    /// 文本项
    /// </summary>
    [JsonPropertyName("text_item")]
    public TextItem? TextItem { get; set; }

    /// <summary>
    /// 图片项
    /// </summary>
    [JsonPropertyName("image_item")]
    public ImageItem? ImageItem { get; set; }

    /// <summary>
    /// 语音项
    /// </summary>
    [JsonPropertyName("voice_item")]
    public VoiceItem? VoiceItem { get; set; }

    /// <summary>
    /// 文件项
    /// </summary>
    [JsonPropertyName("file_item")]
    public FileItem? FileItem { get; set; }

    /// <summary>
    /// 视频项
    /// </summary>
    [JsonPropertyName("video_item")]
    public VideoItem? VideoItem { get; set; }
}

/// <summary>
/// 微信消息（统一消息格式）
/// </summary>
public class WeixinMessage
{
    /// <summary>
    /// 消息序号
    /// </summary>
    [JsonPropertyName("seq")]
    public long? Seq { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonPropertyName("message_id")]
    public long? MessageId { get; set; }

    /// <summary>
    /// 发送者用户ID
    /// </summary>
    [JsonPropertyName("from_user_id")]
    public string? FromUserId { get; set; }

    /// <summary>
    /// 接收者用户ID
    /// </summary>
    [JsonPropertyName("to_user_id")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    /// <summary>
    /// 创建时间(毫秒)
    /// </summary>
    [JsonPropertyName("create_time_ms")]
    public long? CreateTimeMs { get; set; }

    /// <summary>
    /// 更新时间(毫秒)
    /// </summary>
    [JsonPropertyName("update_time_ms")]
    public long? UpdateTimeMs { get; set; }

    /// <summary>
    /// 删除时间(毫秒)
    /// </summary>
    [JsonPropertyName("delete_time_ms")]
    public long? DeleteTimeMs { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    /// <summary>
    /// 群组ID
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonPropertyName("message_type")]
    public int? MessageType { get; set; }

    /// <summary>
    /// 消息状态
    /// </summary>
    [JsonPropertyName("message_state")]
    public int? MessageState { get; set; }

    /// <summary>
    /// 消息项列表
    /// </summary>
    [JsonPropertyName("item_list")]
    public List<MessageItem>? ItemList { get; set; }

    /// <summary>
    /// 上下文令牌
    /// </summary>
    [JsonPropertyName("context_token")]
    public string? ContextToken { get; set; }
}
