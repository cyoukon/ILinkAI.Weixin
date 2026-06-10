# CLAUDE.md

本文件为 Claude 提供项目上下文和工作指南。

## 项目概述

ILinkai Weixin SDK - 用于 ilinkai.weixin.qq.com API 的多语言 SDK。通过 ILinkai 平台实现微信消息的程序化访问。

## 仓库结构

```
ILinkAI.Weixin/
├── CSharp/                          # .NET/C# 实现
│   ├── ILinkai.Weixin.Sdk/          # 核心 SDK 库（NuGet 包）
│   ├── ILinkai.Weixin.Sdk.SqlServer/ # SQL Server 存储扩展（NuGet 包）
│   ├── ILinkai.Weixin.Cli/          # 命令行工具
│   ├── ILinkai.Weixin.Sample/       # 使用示例
│   └── ILinkai.Weixin.slnx          # 解决方案文件
├── Java/                            # Java 实现
├── Python/                          # Python 实现
└── .github/workflows/               # GitHub Actions
```

## 语言特定指南

### C# (.NET 10.0)

- **目标框架**: net10.0
- **主要包**:
  - `ILinkai.Weixin.Sdk` - 核心 SDK
  - `ILinkai.Weixin.Sdk.SqlServer` - SQL Server 存储扩展
- **构建命令**:
  ```bash
  dotnet build CSharp/ILinkai.Weixin.slnx
  dotnet pack CSharp/ILinkai.Weixin.Sdk -c Release
  dotnet pack CSharp/ILinkai.Weixin.Sdk.SqlServer -c Release
  ```
- **测试**: 运行 `ILinkai.Weixin.Sample` 中的示例

### Java

- 使用 Maven 管理依赖
- 配置见 `Java/pom.xml`

### Python

- 使用 setuptools 打包
- 配置见 `Python/setup.py`

## 关键组件

### 认证模块 (`Auth/`)
- `IAccountStore` - 账户存储接口
- `AccountStore` - 文件存储实现
- `SqlServerAccountStore` - SQL Server 存储实现
- `QRCodeLoginService` - 二维码登录流程
- `SessionGuard` - 会话状态管理

### 消息模块 (`Messaging/`)
- `MessageMonitorService` - 长轮询消息接收器
- `MessageSendService` - 消息发送（文本、图片、视频、文件）
- `MessageProcessor` - 消息解析和处理

### CDN/媒体模块 (`Cdn/`, `Media/`)
- `CdnClient` - CDN 上传/下载
- `AesEcbCrypto` - AES-128-ECB 媒体加密
- `MediaUploadService` - 带加密的媒体上传
- `MediaDownloadService` - 带解密的媒体下载

### API 客户端
- `WeixinApiClient` - ilinkai.weixin.qq.com API 核心 HTTP 客户端

## API 端点

基础 URL: `https://ilinkai.weixin.qq.com`

主要端点:
- `/ilink/bot/getupdates` - 长轮询获取消息
- `/ilink/bot/sendmessage` - 发送消息
- `/ilink/bot/getuploadurl` - 获取 CDN 上传 URL
- `/ilink/bot/getconfig` - 获取配置
- `/ilink/bot/sendtyping` - 发送输入状态
- `/ilink/bot/get_bot_qrcode` - 获取登录二维码
- `/ilink/bot/get_qrcode_status` - 检查二维码扫描状态

## 数据模型

### WeixinAccountData
- `Token` - 认证令牌
- `SavedAt` - 保存时间戳
- `BaseUrl` - API 基础 URL
- `UserId` - 关联的微信用户 ID

### WeixinMessage
- `MessageType` - TEXT、IMAGE、VIDEO、FILE、VOICE 等
- `FromUserId` - 发送者 ID
- `ToUserId` - 接收者 ID
- `ItemList` - 消息内容项
- `ContextToken` - 回复用的上下文令牌

## 编码规范

### C#
- 使用可空引用类型（可选参数用 `?`）
- JSON 序列化使用 snake_case_lower（API 要求）
- 公共 API 使用 XML 文档注释
- 通过 `IServiceCollection` 扩展实现依赖注入

### 命名模式
- 服务: `*Service`（如 `MessageSendService`）
- 配置: `*Options`（如 `WeixinApiClientOptions`）
- 异常: `*Exception`（如 `WeixinApiException`）
- 扩展: `ServiceCollectionExtensions`

## NuGet 发布

### 发布流程

#### 1. 更新版本号

两个项目的版本号必须保持一致。修改以下文件中的 `<Version>` 标签：

- `CSharp/ILinkai.Weixin.Sdk/ILinkai.Weixin.Sdk.csproj`
- `CSharp/ILinkai.Weixin.Sdk.SqlServer/ILinkai.Weixin.Sdk.SqlServer.csproj`

```xml
<Version>1.0.5</Version>  <!-- 两个项目使用相同的版本号 -->
```

#### 2. 提交更改

#### 3. 创建并推送 Tag

```bash
git tag v1.0.5
git push origin v1.0.5
```

推送 tag 后，GitHub Action 会自动触发，将两个包发布到 NuGet.org。

#### 4. 手动发布（可选）

也可以通过 GitHub Actions UI 手动触发：
1. 进入仓库的 Actions 页面
2. 选择 "Publish NuGet Packages" 工作流
3. 点击 "Run workflow"
4. 输入版本号和选择要发布的包（sdk、sqlserver 或 both）

### 版本号规则

- 主版本号（Major）：重大架构变更或不兼容的 API 修改
- 次版本号（Minor）：新增功能，向后兼容
- 修订号（Patch）：Bug 修复，向后兼容

### 必需配置

在 GitHub 仓库设置中添加 Secret：
- `NUGET_API_KEY` - NuGet.org 的 API Key

## 常见任务

### 添加新的存储实现
1. 创建新项目，引用 `ILinkai.Weixin.Sdk`
2. 实现 `IAccountStore` 接口
3. 创建 `ServiceCollectionExtensions`，添加 `AddWeixin*Storage()` 方法
4. 更新解决方案文件和 README

### 添加新的消息类型
1. 在 `Enums.cs` 的 `MessageType` 中添加枚举值
2. 更新 `MessageProcessor` 处理新类型
3. 如需要，在 `MessageSendService` 添加发送方法

### 添加新的 API 端点
1. 在 `ApiModels.cs` 创建请求/响应模型
2. 在 `WeixinApiClient` 添加方法
3. 如有复杂逻辑，创建服务方法

## 重要注意事项

- 会话过期（errcode -14）通过 `SessionGuard` 自动暂停
- CDN 媒体需要 AES-128-ECB 加密
- 账户 ID 应通过 `AccountStore.NormalizeAccountId()` 规范化
- 二维码状态轮询使用 `ILink-App-ClientVersion` 头
- 如需要路由，使用 `SKRouteTag` 头