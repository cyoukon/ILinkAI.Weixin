# ILinkai Weixin SDK - Java版

Java版ILinkai微信SDK，功能与C#版完全一致。

## 依赖

- Java 11+
- Jackson (JSON序列化)
- SLF4J (日志)

## 构建

```bash
mvn clean package
```

## 模块结构

```
com.ilinkai.weixin
├── models/          # 数据模型（枚举、消息、API请求/响应）
├── auth/            # 认证（AccountStore、QRCodeLoginService、SessionGuard）
├── cdn/             # CDN操作（AES加密、上传下载）
├── media/           # 媒体服务（上传、下载）
├── messaging/       # 消息服务（发送、监控、处理）
├── WeixinApiClient  # 核心API客户端
└── WeixinApiClientOptions
```

## 快速开始

```java
import com.ilinkai.weixin.*;
import com.ilinkai.weixin.messaging.*;

// 创建客户端
WeixinApiClientOptions opts = new WeixinApiClientOptions();
opts.setToken("your-bot-token");
WeixinApiClient client = new WeixinApiClient(opts);

// 发送文本消息
MessageSendService sender = new MessageSendService(client, WeixinApiClient.DEFAULT_CDN_BASE_URL);
sender.sendText("target-user-id", "Hello!", "context-token");
```
