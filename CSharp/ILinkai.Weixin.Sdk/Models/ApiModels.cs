using System.Text.Json.Serialization;

namespace ILinkai.Weixin.Sdk.Models;

/// <summary>
/// 获取上传URL请求
/// </summary>
public class GetUploadUrlRequest
{
    /// <summary>
    /// 文件键
    /// </summary>
    [JsonPropertyName("filekey")]
    public string? FileKey { get; set; }

    /// <summary>
    /// 媒体类型
    /// </summary>
    [JsonPropertyName("media_type")]
    public int? MediaType { get; set; }

    /// <summary>
    /// 目标用户ID
    /// </summary>
    [JsonPropertyName("to_user_id")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// 原文件明文大小
    /// </summary>
    [JsonPropertyName("rawsize")]
    public int? RawSize { get; set; }

    /// <summary>
    /// 原文件明文MD5
    /// </summary>
    [JsonPropertyName("rawfilemd5")]
    public string? RawFileMd5 { get; set; }

    /// <summary>
    /// 原文件密文大小（AES-128-ECB加密后）
    /// </summary>
    [JsonPropertyName("filesize")]
    public int? FileSize { get; set; }

    /// <summary>
    /// 缩略图明文大小（IMAGE/VIDEO时必填）
    /// </summary>
    [JsonPropertyName("thumb_rawsize")]
    public int? ThumbRawSize { get; set; }

    /// <summary>
    /// 缩略图明文MD5（IMAGE/VIDEO时必填）
    /// </summary>
    [JsonPropertyName("thumb_rawfilemd5")]
    public string? ThumbRawFileMd5 { get; set; }

    /// <summary>
    /// 缩略图密文大小（IMAGE/VIDEO时必填）
    /// </summary>
    [JsonPropertyName("thumb_filesize")]
    public int? ThumbFileSize { get; set; }

    /// <summary>
    /// 不需要缩略图上传URL，默认false
    /// </summary>
    [JsonPropertyName("no_need_thumb")]
    public bool? NoNeedThumb { get; set; }

    /// <summary>
    /// 加密密钥
    /// </summary>
    [JsonPropertyName("aeskey")]
    public string? AesKey { get; set; }

    /// <summary>
    /// 基础信息
    /// </summary>
    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// 获取上传URL响应
/// </summary>
public class GetUploadUrlResponse
{
    /// <summary>
    /// 原图上传加密参数
    /// </summary>
    [JsonPropertyName("upload_param")]
    public string? UploadParam { get; set; }

    /// <summary>
    /// 缩略图上传加密参数，无缩略图时为空
    /// </summary>
    [JsonPropertyName("thumb_upload_param")]
    public string? ThumbUploadParam { get; set; }
}

/// <summary>
/// 获取更新请求
/// </summary>
public class GetUpdatesRequest
{
    /// <summary>
    /// 同步缓冲区（已弃用）
    /// </summary>
    [JsonPropertyName("sync_buf")]
    [Obsolete("已弃用，请使用GetUpdatesBuf")]
    public string? SyncBuf { get; set; }

    /// <summary>
    /// 完整上下文缓冲区，本地缓存；无缓存时发送空字符串
    /// </summary>
    [JsonPropertyName("get_updates_buf")]
    public string? GetUpdatesBuf { get; set; }

    /// <summary>
    /// 基础信息
    /// </summary>
    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// 获取更新响应
/// </summary>
public class GetUpdatesResponse
{
    /// <summary>
    /// 返回码
    /// </summary>
    [JsonPropertyName("ret")]
    public int? Ret { get; set; }

    /// <summary>
    /// 错误码（例如-14表示会话超时）
    /// </summary>
    [JsonPropertyName("errcode")]
    public int? ErrCode { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }

    /// <summary>
    /// 消息列表
    /// </summary>
    [JsonPropertyName("msgs")]
    public List<WeixinMessage>? Messages { get; set; }

    /// <summary>
    /// 同步缓冲区（已弃用）
    /// </summary>
    [JsonPropertyName("sync_buf")]
    [Obsolete("已弃用，请使用GetUpdatesBuf")]
    public string? SyncBuf { get; set; }

    /// <summary>
    /// 完整上下文缓冲区，需本地缓存并在下次请求时发送
    /// </summary>
    [JsonPropertyName("get_updates_buf")]
    public string? GetUpdatesBuf { get; set; }

    /// <summary>
    /// 服务器建议的下一次长轮询超时时间(毫秒)
    /// </summary>
    [JsonPropertyName("longpolling_timeout_ms")]
    public int? LongPollingTimeoutMs { get; set; }
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    [JsonPropertyName("msg")]
    public WeixinMessage? Message { get; set; }
}

/// <summary>
/// 发送消息响应
/// </summary>
public class SendMessageResponse
{
}

/// <summary>
/// 发送输入状态请求
/// </summary>
public class SendTypingRequest
{
    /// <summary>
    /// ILink用户ID
    /// </summary>
    [JsonPropertyName("ilink_user_id")]
    public string? ILinkUserId { get; set; }

    /// <summary>
    /// 输入票据
    /// </summary>
    [JsonPropertyName("typing_ticket")]
    public string? TypingTicket { get; set; }

    /// <summary>
    /// 状态：1=正在输入（默认），2=取消输入
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// 基础信息
    /// </summary>
    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// 发送输入状态响应
/// </summary>
public class SendTypingResponse
{
    /// <summary>
    /// 返回码
    /// </summary>
    [JsonPropertyName("ret")]
    public int? Ret { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }
}

/// <summary>
/// 获取配置请求
/// </summary>
public class GetConfigRequest
{
    /// <summary>
    /// ILink用户ID
    /// </summary>
    [JsonPropertyName("ilink_user_id")]
    public string? ILinkUserId { get; set; }

    /// <summary>
    /// 上下文令牌
    /// </summary>
    [JsonPropertyName("context_token")]
    public string? ContextToken { get; set; }

    /// <summary>
    /// 基础信息
    /// </summary>
    [JsonPropertyName("base_info")]
    public BaseInfo? BaseInfo { get; set; }
}

/// <summary>
/// 获取配置响应
/// </summary>
public class GetConfigResponse
{
    /// <summary>
    /// 返回码
    /// </summary>
    [JsonPropertyName("ret")]
    public int? Ret { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }

    /// <summary>
    /// Base64编码的输入票据，用于sendTyping
    /// </summary>
    [JsonPropertyName("typing_ticket")]
    public string? TypingTicket { get; set; }
}

/// <summary>
/// 获取二维码响应
/// </summary>
public class GetQRCodeResponse
{
    /// <summary>
    /// 二维码标识
    /// </summary>
    [JsonPropertyName("qrcode")]
    public string? QRCode { get; set; }

    /// <summary>
    /// 二维码图片内容（URL）
    /// </summary>
    [JsonPropertyName("qrcode_img_content")]
    public string? QRCodeImgContent { get; set; }
}

/// <summary>
/// 获取二维码状态响应
/// </summary>
public class GetQRCodeStatusResponse
{
    /// <summary>
    /// 状态：wait/scaned/confirmed/expired
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// 机器人令牌
    /// </summary>
    [JsonPropertyName("bot_token")]
    public string? BotToken { get; set; }

    /// <summary>
    /// ILink机器人ID
    /// </summary>
    [JsonPropertyName("ilink_bot_id")]
    public string? ILinkBotId { get; set; }

    /// <summary>
    /// 基础URL
    /// </summary>
    [JsonPropertyName("baseurl")]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 扫码用户的用户ID
    /// </summary>
    [JsonPropertyName("ilink_user_id")]
    public string? ILinkUserId { get; set; }
}
