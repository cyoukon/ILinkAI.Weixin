# ILinkai Weixin SDK - Python版

Python版ILinkai微信SDK，功能与C#版完全一致。零第三方依赖。

## 依赖

- Python 3.8+
- 无第三方依赖（AES加密为纯Python实现）

## 安装

```bash
pip install -e .
```

## 模块结构

```
ilinkai_weixin/
├── models.py              # 数据模型
├── weixin_api_client.py   # 核心API客户端
├── auth/                  # 认证
│   ├── account_store.py
│   ├── qrcode_login_service.py
│   └── session_guard.py
├── cdn/                   # CDN操作
│   ├── aes_ecb_crypto.py
│   ├── cdn_client.py
│   └── cdn_url_builder.py
├── media/                 # 媒体服务
│   ├── media_download_service.py
│   └── media_upload_service.py
└── messaging/             # 消息服务
    ├── message_monitor_service.py
    ├── message_processor.py
    └── message_send_service.py
```

## 快速开始

```python
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.messaging import MessageSendService

# 创建客户端
client = WeixinApiClient(WeixinApiClientOptions(token="your-bot-token"))

# 发送文本消息
sender = MessageSendService(client, "https://novac2c.cdn.weixin.qq.com/c2c")
sender.send_text("target-user-id", "Hello!", "context-token")
```
