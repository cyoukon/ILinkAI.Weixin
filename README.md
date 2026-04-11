# ILinkai Weixin SDK

ILinkai微信SDK - 用于ilinkai.weixin.qq.com API的完整C#开发包

> C# 包已经发布到nuget.org，[点击这里查看](https://www.nuget.org/packages/ILinkai.Weixin.Sdk/)，Python 和 Java 版本需要自行从源码构建

## 功能特性

- ✅ 完整的API封装（getUpdates、sendMessage、getUploadUrl等）
- ✅ 二维码扫码登录
- ✅ 消息收发（文本、图片、视频、文件、语音）
- ✅ CDN文件上传下载（AES-128-ECB加密）
- ✅ 长轮询消息监控
- ✅ 账户管理和持久化存储
- ✅ 会话状态管理
- ✅ 命令行工具(CLI)

## 安装

### NuGet包

```bash
dotnet add package ILinkai.Weixin.Sdk
```

### CLI工具

```bash
dotnet tool install -g ILinkai.Weixin.Cli
```

## 快速开始

### 1. 扫码登录

```csharp
using ILinkai.Weixin.Sdk.Auth;

var loginService = new QRCodeLoginService("https://ilinkai.weixin.qq.com");

// 获取二维码
var startResult = await loginService.StartLoginAsync();
Console.WriteLine($"请扫描: {startResult.QRCodeUrl}");

// 等待登录完成
var waitResult = await loginService.WaitForLoginAsync(startResult.QRCode);

if (waitResult.Connected)
{
    Console.WriteLine($"登录成功！Token: {waitResult.BotToken}");
    
    // 保存账户
    var accountStore = new AccountStore();
    accountStore.SaveAccount(waitResult.AccountId, new WeixinAccountData
    {
        Token = waitResult.BotToken,
        BaseUrl = waitResult.BaseUrl
    });
}
```

### 2. 发送消息

```csharp
using ILinkai.Weixin.Sdk;
using ILinkai.Weixin.Sdk.Messaging;

var apiClient = new WeixinApiClient(new WeixinApiClientOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    Token = "your-bot-token"
});

var sendService = new MessageSendService(apiClient, "https://novac2c.cdn.weixin.qq.com/c2c");

// 发送文本
await sendService.SendTextAsync(
    toUserId: "user@im.wechat",
    text: "Hello!",
    contextToken: "context-token-from-message");

// 发送图片
await sendService.SendImageAsync(
    toUserId: "user@im.wechat",
    filePath: "/path/to/image.jpg",
    contextToken: "context-token-from-message",
    caption: "图片说明");
```

### 3. 监控消息

```csharp
using ILinkai.Weixin.Sdk.Messaging;

var options = new MessageMonitorOptions
{
    BaseUrl = "https://ilinkai.weixin.qq.com",
    Token = "your-bot-token",
    AccountId = "your-account-id"
};

using var monitor = new MessageMonitorService(options);

monitor.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"收到消息: {e.Context.Body}");
    Console.WriteLine($"发送者: {e.Context.From}");
    Console.WriteLine($"上下文令牌: {e.Context.ContextToken}");
};

monitor.ErrorOccurred += (sender, e) =>
{
    Console.WriteLine($"错误: {e.Error.Message}");
};

await monitor.StartAsync();
```

### 4. 上传文件

```csharp
using ILinkai.Weixin.Sdk.Media;

var uploadService = new MediaUploadService(apiClient, cdnBaseUrl);

var uploaded = await uploadService.UploadImageAsync(
    filePath: "/path/to/image.jpg",
    toUserId: "user@im.wechat");

Console.WriteLine($"文件键: {uploaded.FileKey}");
Console.WriteLine($"下载参数: {uploaded.DownloadEncryptedQueryParam}");
```

### 5. 下载媒体

```csharp
var downloadService = new MediaDownloadService(cdnBaseUrl);

var imageData = await downloadService.DownloadImageAsync(
    encryptQueryParam: "encrypted-param",
    aesKeyBase64: "base64-encoded-aes-key");

await downloadService.SaveMediaToFileAsync(imageData, "/save/path", "image.jpg");
```

## CLI使用

### 安装插件并登录

```bash
ilinkai-weixin install
```

### 扫码登录

```bash
ilinkai-weixin login --base-url https://ilinkai.weixin.qq.com
```

### 发送消息

```bash
ilinkai-weixin send --to "user@im.wechat" --text "Hello" --token "your-token" --context-token "ctx-token"
```

### 监控消息

```bash
ilinkai-weixin monitor --account-id "your-account-id" --token "your-token"
```

### 账户管理

```bash
# 列出所有账户
ilinkai-weixin account list

# 显示账户详情
ilinkai-weixin account show "account-id"
```

## API参考

### WeixinApiClient

核心API客户端，提供与ilinkai.weixin.qq.com交互的所有方法。

| 方法                  | 说明         |
| ------------------- | ---------- |
| `GetUpdatesAsync`   | 长轮询获取新消息   |
| `SendMessageAsync`  | 发送消息       |
| `GetUploadUrlAsync` | 获取CDN上传URL |
| `GetConfigAsync`    | 获取配置信息     |
| `SendTypingAsync`   | 发送输入状态     |

### QRCodeLoginService

二维码登录服务。

| 方法                  | 说明         |
| ------------------- | ---------- |
| `StartLoginAsync`   | 开始登录，获取二维码 |
| `WaitForLoginAsync` | 等待扫码确认     |

### MessageMonitorService

消息监控服务，长轮询接收消息。

| 事件                | 说明       |
| ----------------- | -------- |
| `MessageReceived` | 收到新消息时触发 |
| `ErrorOccurred`   | 发生错误时触发  |
| `StatusChanged`   | 状态变化时触发  |

### MessageSendService

消息发送服务。

| 方法                | 说明     |
| ----------------- | ------ |
| `SendTextAsync`   | 发送文本消息 |
| `SendImageAsync`  | 发送图片   |
| `SendVideoAsync`  | 发送视频   |
| `SendFileAsync`   | 发送文件   |
| `SendTypingAsync` | 发送输入状态 |

### MediaUploadService / MediaDownloadService

媒体文件上传下载服务，自动处理AES加密。

## 配置选项

### WeixinApiClientOptions

| 属性                  | 类型      | 默认值                                     | 说明        |
| ------------------- | ------- | --------------------------------------- | --------- |
| `BaseUrl`           | string  | <https://ilinkai.weixin.qq.com>         | API基础URL  |
| `CdnBaseUrl`        | string  | <https://novac2c.cdn.weixin.qq.com/c2c> | CDN基础URL  |
| `Token`             | string? | null                                    | 认证令牌      |
| `TimeoutMs`         | int     | 15000                                   | 请求超时(毫秒)  |
| `LongPollTimeoutMs` | int     | 35000                                   | 长轮询超时(毫秒) |

## 错误处理

SDK定义了以下异常类型：

- `WeixinApiException` - API请求错误
- `WeixinSessionPausedException` - 会话已暂停
- `CdnUploadException` - CDN上传错误
- `CdnDownloadException` - CDN下载错误

```csharp
try
{
    await sendService.SendTextAsync(to, text, contextToken);
}
catch (WeixinSessionPausedException ex)
{
    Console.WriteLine($"会话已暂停，{ex.RemainingMinutes}分钟后重试");
}
catch (WeixinApiException ex)
{
    Console.WriteLine($"API错误: {ex.Message}, StatusCode: {ex.StatusCode}");
}
```

## 项目结构

```
ilinkai-sdk/
├── src/
│   ├── ILinkai.Weixin.Sdk/           # SDK核心库
│   │   ├── Models/                   # 数据模型
│   │   ├── Auth/                     # 认证模块
│   │   ├── Cdn/                      # CDN加密/上传/下载
│   │   ├── Media/                    # 媒体处理
│   │   ├── Messaging/                # 消息收发
│   │   └── WeixinApiClient.cs        # API客户端
│   └── ILinkai.Weixin.Cli/           # 命令行工具
├── examples/                         # 使用示例
└── ILinkai.Weixin.sln               # 解决方案文件
```

## 许可证

MIT License