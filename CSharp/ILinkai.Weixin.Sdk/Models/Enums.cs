using System.Text.Json.Serialization;

namespace ILinkai.Weixin.Sdk.Models;

/// <summary>
/// 上传媒体类型枚举
/// </summary>
public enum UploadMediaType
{
    /// <summary>
    /// 图片类型
    /// </summary>
    Image = 1,

    /// <summary>
    /// 视频类型
    /// </summary>
    Video = 2,

    /// <summary>
    /// 文件类型
    /// </summary>
    File = 3,

    /// <summary>
    /// 语音类型
    /// </summary>
    Voice = 4
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 无类型
    /// </summary>
    None = 0,

    /// <summary>
    /// 用户消息
    /// </summary>
    User = 1,

    /// <summary>
    /// 机器人消息
    /// </summary>
    Bot = 2
}

/// <summary>
/// 消息项类型枚举
/// </summary>
public enum MessageItemType
{
    /// <summary>
    /// 无类型
    /// </summary>
    None = 0,

    /// <summary>
    /// 文本消息
    /// </summary>
    Text = 1,

    /// <summary>
    /// 图片消息
    /// </summary>
    Image = 2,

    /// <summary>
    /// 语音消息
    /// </summary>
    Voice = 3,

    /// <summary>
    /// 文件消息
    /// </summary>
    File = 4,

    /// <summary>
    /// 视频消息
    /// </summary>
    Video = 5
}

/// <summary>
/// 消息状态枚举
/// </summary>
public enum MessageState
{
    /// <summary>
    /// 新消息
    /// </summary>
    New = 0,

    /// <summary>
    /// 生成中
    /// </summary>
    Generating = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Finish = 2
}

/// <summary>
/// 输入状态枚举
/// </summary>
public enum TypingStatus
{
    /// <summary>
    /// 正在输入
    /// </summary>
    Typing = 1,

    /// <summary>
    /// 取消输入
    /// </summary>
    Cancel = 2
}

/// <summary>
/// 语音编码类型枚举
/// </summary>
public enum VoiceEncodeType
{
    /// <summary>
    /// PCM格式
    /// </summary>
    Pcm = 1,

    /// <summary>
    /// ADPCM格式
    /// </summary>
    Adpcm = 2,

    /// <summary>
    /// Feature格式
    /// </summary>
    Feature = 3,

    /// <summary>
    /// Speex格式
    /// </summary>
    Speex = 4,

    /// <summary>
    /// AMR格式
    /// </summary>
    Amr = 5,

    /// <summary>
    /// SILK格式
    /// </summary>
    Silk = 6,

    /// <summary>
    /// MP3格式
    /// </summary>
    Mp3 = 7,

    /// <summary>
    /// OGG-Speex格式
    /// </summary>
    OggSpeex = 8
}

/// <summary>
/// 二维码登录状态枚举
/// </summary>
public enum QRCodeStatus
{
    /// <summary>
    /// 等待扫码
    /// </summary>
    Wait,

    /// <summary>
    /// 已扫码
    /// </summary>
    Scanned,

    /// <summary>
    /// 已确认
    /// </summary>
    Confirmed,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired
}
