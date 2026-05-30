using System.Text;
using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sdk.Messaging;

/// <summary>
/// 消息发送服务
/// 处理向微信发送消息
/// </summary>
public class MessageSendService
{
    private readonly WeixinApiClient _apiClient;
    private readonly Media.MediaUploadService _uploadService;
    private readonly string _cdnBaseUrl;
    private readonly ILogger? _logger;

    /// <summary>
    /// 创建MessageSendService实例
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="cdnBaseUrl">CDN基础URL</param>
    /// <param name="logger">日志记录器</param>
    public MessageSendService(WeixinApiClient apiClient, string cdnBaseUrl, ILogger? logger = null)
    {
        _apiClient = apiClient;
        _cdnBaseUrl = cdnBaseUrl;
        _logger = logger;
        _uploadService = new Media.MediaUploadService(apiClient, cdnBaseUrl, logger);
    }

    /// <summary>
    /// 生成客户端ID
    /// </summary>
    private static string GenerateClientId()
    {
        return $"ilinkai-weixin-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    /// <param name="toUserId">目标用户ID</param>
    /// <param name="text">文本内容</param>
    /// <param name="contextToken">上下文令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息ID</returns>
    public async Task<string> SendTextAsync(
        string toUserId,
        string text,
        string contextToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contextToken))
        {
            _logger?.LogError("SendText: contextToken missing, refusing to send to={ToUserId}", toUserId);
            throw new ArgumentException("contextToken is required", nameof(contextToken));
        }

        var clientId = GenerateClientId();
        var request = new Models.SendMessageRequest
        {
            Message = new Models.WeixinMessage
            {
                FromUserId = "",
                ToUserId = toUserId,
                ClientId = clientId,
                MessageType = (int)Models.MessageType.Bot,
                MessageState = (int)Models.MessageState.Finish,
                ItemList = string.IsNullOrEmpty(text)
                    ? new List<Models.MessageItem>()
                    : new List<Models.MessageItem>
                    {
                        new Models.MessageItem
                        {
                            Type = (int)Models.MessageItemType.Text,
                            TextItem = new Models.TextItem { Text = text }
                        }
                    },
                ContextToken = contextToken
            }
        };

        try
        {
            await _apiClient.SendMessageAsync(request, cancellationToken);
            _logger?.LogDebug("SendText: success to={ToUserId} clientId={ClientId}", toUserId, clientId);
            return clientId;
        }
        catch (Exception err)
        {
            _logger?.LogError(err, "SendText: failed to={ToUserId} clientId={ClientId}", toUserId, clientId);
            throw;
        }
    }

    /// <summary>
    /// 发送图片消息
    /// </summary>
    /// <param name="toUserId">目标用户ID</param>
    /// <param name="filePath">本地图片路径</param>
    /// <param name="contextToken">上下文令牌</param>
    /// <param name="caption">图片说明（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息ID</returns>
    public async Task<string> SendImageAsync(
        string toUserId,
        string filePath,
        string contextToken,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contextToken))
        {
            _logger?.LogError("SendImage: contextToken missing, refusing to send to={ToUserId}", toUserId);
            throw new ArgumentException("contextToken is required", nameof(contextToken));
        }

        _logger?.LogDebug("SendImage: to={ToUserId} filePath={FilePath}", toUserId, filePath);

        var uploaded = await _uploadService.UploadImageAsync(filePath, toUserId, cancellationToken);

        var imageItem = new Models.MessageItem
        {
            Type = (int)Models.MessageItemType.Image,
            ImageItem = new Models.ImageItem
            {
                Media = new Models.CdnMedia
                {
                    EncryptQueryParam = uploaded.DownloadEncryptedQueryParam,
                    AesKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(uploaded.AesKey)),
                    EncryptType = 1
                },
                MidSize = uploaded.FileSizeCiphertext
            }
        };

        return await SendMediaItemsAsync(toUserId, caption, imageItem, contextToken, "SendImage", cancellationToken);
    }

    /// <summary>
    /// 发送视频消息
    /// </summary>
    /// <param name="toUserId">目标用户ID</param>
    /// <param name="filePath">本地视频路径</param>
    /// <param name="contextToken">上下文令牌</param>
    /// <param name="caption">视频说明（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息ID</returns>
    public async Task<string> SendVideoAsync(
        string toUserId,
        string filePath,
        string contextToken,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contextToken))
        {
            _logger?.LogError("SendVideo: contextToken missing, refusing to send to={ToUserId}", toUserId);
            throw new ArgumentException("contextToken is required", nameof(contextToken));
        }

        var uploaded = await _uploadService.UploadVideoAsync(filePath, toUserId, cancellationToken);

        var videoItem = new Models.MessageItem
        {
            Type = (int)Models.MessageItemType.Video,
            VideoItem = new Models.VideoItem
            {
                Media = new Models.CdnMedia
                {
                    EncryptQueryParam = uploaded.DownloadEncryptedQueryParam,
                    AesKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(uploaded.AesKey)),
                    EncryptType = 1
                },
                VideoSize = uploaded.FileSizeCiphertext
            }
        };

        return await SendMediaItemsAsync(toUserId, caption, videoItem, contextToken, "SendVideo", cancellationToken);
    }

    /// <summary>
    /// 发送文件消息
    /// </summary>
    /// <param name="toUserId">目标用户ID</param>
    /// <param name="filePath">本地文件路径</param>
    /// <param name="contextToken">上下文令牌</param>
    /// <param name="caption">文件说明（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息ID</returns>
    public async Task<string> SendFileAsync(
        string toUserId,
        string filePath,
        string contextToken,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contextToken))
        {
            _logger?.LogError("SendFile: contextToken missing, refusing to send to={ToUserId}", toUserId);
            throw new ArgumentException("contextToken is required", nameof(contextToken));
        }

        var fileName = Path.GetFileName(filePath);
        var uploaded = await _uploadService.UploadFileAttachmentAsync(filePath, toUserId, cancellationToken);

        var fileItem = new Models.MessageItem
        {
            Type = (int)Models.MessageItemType.File,
            FileItem = new Models.FileItem
            {
                Media = new Models.CdnMedia
                {
                    EncryptQueryParam = uploaded.DownloadEncryptedQueryParam,
                    AesKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(uploaded.AesKey)),
                    EncryptType = 1
                },
                FileName = fileName,
                Length = uploaded.FileSize.ToString()
            }
        };

        return await SendMediaItemsAsync(toUserId, caption, fileItem, contextToken, "SendFile", cancellationToken);
    }

    /// <summary>
    /// 发送媒体项
    /// </summary>
    private async Task<string> SendMediaItemsAsync(
        string toUserId,
        string? text,
        Models.MessageItem mediaItem,
        string contextToken,
        string label,
        CancellationToken cancellationToken)
    {
        var items = new List<Models.MessageItem>();

        if (!string.IsNullOrEmpty(text))
        {
            items.Add(new Models.MessageItem
            {
                Type = (int)Models.MessageItemType.Text,
                TextItem = new Models.TextItem { Text = text }
            });
        }

        items.Add(mediaItem);

        var lastClientId = "";
        foreach (var item in items)
        {
            lastClientId = GenerateClientId();
            var request = new Models.SendMessageRequest
            {
                Message = new Models.WeixinMessage
                {
                    FromUserId = "",
                    ToUserId = toUserId,
                    ClientId = lastClientId,
                    MessageType = (int)Models.MessageType.Bot,
                    MessageState = (int)Models.MessageState.Finish,
                    ItemList = new List<Models.MessageItem> { item },
                    ContextToken = contextToken
                }
            };

            try
            {
                await _apiClient.SendMessageAsync(request, cancellationToken);
            }
            catch (Exception err)
            {
                _logger?.LogError(err, "{Label}: failed to={ToUserId} clientId={ClientId}", label, toUserId, lastClientId);
                throw;
            }
        }

        _logger?.LogDebug("{Label}: success to={ToUserId} clientId={ClientId}", label, toUserId, lastClientId);
        return lastClientId;
    }

    /// <summary>
    /// 发送输入状态
    /// </summary>
    /// <param name="ilinkUserId">ILink用户ID</param>
    /// <param name="typingTicket">输入票据</param>
    /// <param name="isTyping">是否正在输入</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendTypingAsync(
        string ilinkUserId,
        string typingTicket,
        bool isTyping = true,
        CancellationToken cancellationToken = default)
    {
        var request = new Models.SendTypingRequest
        {
            ILinkUserId = ilinkUserId,
            TypingTicket = typingTicket,
            Status = isTyping ? (int)Models.TypingStatus.Typing : (int)Models.TypingStatus.Cancel
        };

        await _apiClient.SendTypingAsync(request, cancellationToken);
    }
}
