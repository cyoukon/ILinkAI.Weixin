using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Messaging;

/// <summary>
/// 消息上下文信息
/// 从微信消息转换而来的统一消息格式
/// </summary>
public class MessageContext
{
    /// <summary>
    /// 消息体（文本内容）
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// 发送者ID
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// 接收者ID
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// 消息ID
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// 上下文令牌
    /// </summary>
    public string? ContextToken { get; set; }

    /// <summary>
    /// 媒体文件路径（已下载和解密）
    /// </summary>
    public string? MediaPath { get; set; }

    /// <summary>
    /// 媒体类型
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// 原始消息
    /// </summary>
    public Models.WeixinMessage? OriginalMessage { get; set; }
}

/// <summary>
/// 消息处理服务
/// 处理接收到的微信消息
/// </summary>
public class MessageProcessor
{
    private readonly Media.MediaDownloadService _downloadService;
    private readonly ILogger? _logger;

    /// <summary>
    /// 创建MessageProcessor实例
    /// </summary>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="logger">日志记录器</param>
    public MessageProcessor(string cdnBaseUrl, ILogger? logger = null)
    {
        _downloadService = new Media.MediaDownloadService(cdnBaseUrl, logger);
        _logger = logger;
    }

    /// <summary>
    /// 将微信消息转换为消息上下文
    /// </summary>
    /// <param name="message">微信消息</param>
    /// <param name="accountId">账户ID</param>
    /// <returns>消息上下文</returns>
    public MessageContext ConvertToContext(Models.WeixinMessage message, string accountId)
    {
        var fromUserId = message.FromUserId ?? "";
        var body = ExtractBodyFromItemList(message.ItemList);

        return new MessageContext
        {
            Body = body,
            From = fromUserId,
            To = fromUserId,
            AccountId = accountId,
            MessageId = $"ilinkai-{Guid.NewGuid():N}",
            Timestamp = message.CreateTimeMs,
            ContextToken = message.ContextToken,
            OriginalMessage = message
        };
    }

    /// <summary>
    /// 处理消息中的媒体内容
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <param name="mediaDir">媒体存储目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ProcessMediaAsync(MessageContext context, string mediaDir, CancellationToken cancellationToken = default)
    {
        var message = context.OriginalMessage;
        if (message?.ItemList == null || message.ItemList.Count == 0)
        {
            return;
        }

        foreach (var item in message.ItemList)
        {
            if (item.Type == null) continue;

            try
            {
                switch ((Models.MessageItemType)item.Type)
                {
                    case Models.MessageItemType.Image:
                        await ProcessImageItemAsync(context, item, mediaDir, cancellationToken);
                        break;

                    case Models.MessageItemType.Voice:
                        await ProcessVoiceItemAsync(context, item, mediaDir, cancellationToken);
                        break;

                    case Models.MessageItemType.File:
                        await ProcessFileItemAsync(context, item, mediaDir, cancellationToken);
                        break;

                    case Models.MessageItemType.Video:
                        await ProcessVideoItemAsync(context, item, mediaDir, cancellationToken);
                        break;
                }

                if (!string.IsNullOrEmpty(context.MediaPath))
                {
                    break;
                }
            }
            catch (Exception err)
            {
                _logger?.LogError(err, "ProcessMedia: failed to process item type={Type}", item.Type);
            }
        }
    }

    /// <summary>
    /// 处理图片项
    /// </summary>
    private async Task ProcessImageItemAsync(MessageContext context, Models.MessageItem item, string mediaDir, CancellationToken cancellationToken)
    {
        var img = item.ImageItem;
        if (img?.Media?.EncryptQueryParam == null) return;

        var aesKeyBase64 = !string.IsNullOrEmpty(img.AesKey)
            ? Convert.ToBase64String(Convert.FromHexString(img.AesKey))
            : img.Media.AesKey;

        _logger?.LogDebug("ProcessImage: hasAesKey={HasKey}", !string.IsNullOrEmpty(aesKeyBase64));

        try
        {
            var data = await _downloadService.DownloadImageAsync(img.Media.EncryptQueryParam, aesKeyBase64, cancellationToken);
            context.MediaPath = await _downloadService.SaveMediaToFileAsync(data, mediaDir, $"image_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg", cancellationToken);
            context.MediaType = "image/*";
            _logger?.LogDebug("ProcessImage: saved to {Path}", context.MediaPath);
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "ProcessImage: download/decrypt failed");
        }
    }

    /// <summary>
    /// 处理语音项
    /// </summary>
    private async Task ProcessVoiceItemAsync(MessageContext context, Models.MessageItem item, string mediaDir, CancellationToken cancellationToken)
    {
        var voice = item.VoiceItem;
        if (voice?.Media?.EncryptQueryParam == null || string.IsNullOrEmpty(voice.Media.AesKey)) return;

        try
        {
            var data = await _downloadService.DownloadVoiceAsync(voice.Media.EncryptQueryParam, voice.Media.AesKey, cancellationToken);
            context.MediaPath = await _downloadService.SaveMediaToFileAsync(data, mediaDir, $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}.silk", cancellationToken);
            context.MediaType = "audio/silk";
            _logger?.LogDebug("ProcessVoice: saved to {Path}", context.MediaPath);
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "ProcessVoice: download failed");
        }
    }

    /// <summary>
    /// 处理文件项
    /// </summary>
    private async Task ProcessFileItemAsync(MessageContext context, Models.MessageItem item, string mediaDir, CancellationToken cancellationToken)
    {
        var fileItem = item.FileItem;
        if (fileItem?.Media?.EncryptQueryParam == null || string.IsNullOrEmpty(fileItem.Media.AesKey)) return;

        try
        {
            var data = await _downloadService.DownloadFileAsync(fileItem.Media.EncryptQueryParam, fileItem.Media.AesKey, cancellationToken);
            var fileName = !string.IsNullOrEmpty(fileItem.FileName) ? fileItem.FileName : $"file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";
            context.MediaPath = await _downloadService.SaveMediaToFileAsync(data, mediaDir, fileName, cancellationToken);
            context.MediaType = GetMimeTypeFromFileName(fileName);
            _logger?.LogDebug("ProcessFile: saved to {Path}", context.MediaPath);
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "ProcessFile: download failed");
        }
    }

    /// <summary>
    /// 处理视频项
    /// </summary>
    private async Task ProcessVideoItemAsync(MessageContext context, Models.MessageItem item, string mediaDir, CancellationToken cancellationToken)
    {
        var videoItem = item.VideoItem;
        if (videoItem?.Media?.EncryptQueryParam == null || string.IsNullOrEmpty(videoItem.Media.AesKey)) return;

        try
        {
            var data = await _downloadService.DownloadVideoAsync(videoItem.Media.EncryptQueryParam, videoItem.Media.AesKey, cancellationToken);
            context.MediaPath = await _downloadService.SaveMediaToFileAsync(data, mediaDir, $"video_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4", cancellationToken);
            context.MediaType = "video/mp4";
            _logger?.LogDebug("ProcessVideo: saved to {Path}", context.MediaPath);
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "ProcessVideo: download failed");
        }
    }

    /// <summary>
    /// 从消息项列表提取文本内容
    /// </summary>
    public static string ExtractBodyFromItemList(List<Models.MessageItem>? itemList)
    {
        if (itemList == null || itemList.Count == 0) return string.Empty;

        foreach (var item in itemList)
        {
            if (item.Type == (int)Models.MessageItemType.Text && item.TextItem?.Text != null)
            {
                var text = item.TextItem.Text;
                var refMsg = item.RefMessage;

                if (refMsg == null) return text;

                if (refMsg.MessageItem != null && IsMediaItem(refMsg.MessageItem))
                {
                    return text;
                }

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(refMsg.Title))
                {
                    parts.Add(refMsg.Title);
                }
                if (refMsg.MessageItem != null)
                {
                    var refBody = ExtractBodyFromItemList(new List<Models.MessageItem> { refMsg.MessageItem });
                    if (!string.IsNullOrEmpty(refBody))
                    {
                        parts.Add(refBody);
                    }
                }

                if (parts.Count == 0) return text;
                return $"[引用: {string.Join(" | ", parts)}]\n{text}";
            }

            if (item.Type == (int)Models.MessageItemType.Voice && item.VoiceItem?.Text != null)
            {
                return item.VoiceItem.Text;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查是否为媒体项
    /// </summary>
    public static bool IsMediaItem(Models.MessageItem item)
    {
        return item.Type == (int)Models.MessageItemType.Image ||
               item.Type == (int)Models.MessageItemType.Video ||
               item.Type == (int)Models.MessageItemType.File ||
               item.Type == (int)Models.MessageItemType.Voice;
    }

    /// <summary>
    /// 根据文件名获取MIME类型
    /// </summary>
    private static string GetMimeTypeFromFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}
