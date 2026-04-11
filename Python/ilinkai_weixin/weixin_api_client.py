"""ILinkai微信API客户端"""
import json
import logging
import os
import struct
import base64
from dataclasses import dataclass, field
from typing import Optional
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

from .models import (
    BaseInfo, GetUpdatesRequest, GetUpdatesResponse, GetUploadUrlRequest,
    GetUploadUrlResponse, SendMessageRequest, GetConfigRequest, GetConfigResponse,
    SendTypingRequest, WeixinMessage, to_dict, parse_weixin_message,
)

logger = logging.getLogger(__name__)


class WeixinApiException(Exception):
    def __init__(self, message: str, status_code: int = 0):
        super().__init__(message)
        self.status_code = status_code


@dataclass
class WeixinApiClientOptions:
    base_url: str = "https://ilinkai.weixin.qq.com"
    cdn_base_url: str = "https://novac2c.cdn.weixin.qq.com/c2c"
    token: Optional[str] = None
    timeout_ms: int = 15000
    long_poll_timeout_ms: int = 35000
    config_timeout_ms: int = 10000
    route_tag: Optional[str] = None

DEFAULT_BASE_URL = "https://ilinkai.weixin.qq.com"
DEFAULT_CDN_BASE_URL = "https://novac2c.cdn.weixin.qq.com/c2c"
SESSION_EXPIRED_ERROR_CODE = -14


class WeixinApiClient:
    """ILinkai微信API客户端"""

    def __init__(self, options: Optional[WeixinApiClientOptions] = None):
        self.options = options or WeixinApiClientOptions()
        self._channel_version = self._get_channel_version()

    @staticmethod
    def _get_channel_version() -> str:
        return "1.0.0"

    def _build_base_info(self) -> BaseInfo:
        return BaseInfo(channel_version=self._channel_version)

    @staticmethod
    def _generate_random_wechat_uin() -> str:
        random_bytes = os.urandom(4)
        uint32_value = struct.unpack('<I', random_bytes)[0]
        decimal_string = str(uint32_value)
        return base64.b64encode(decimal_string.encode('utf-8')).decode('ascii')

    @staticmethod
    def _ensure_trailing_slash(url: str) -> str:
        return url if url.endswith('/') else url + '/'

    def _build_headers(self, body: bytes) -> dict:
        headers = {
            'Content-Type': 'application/json',
            'AuthorizationType': 'ilink_bot_token',
            'Content-Length': str(len(body)),
            'X-WECHAT-UIN': self._generate_random_wechat_uin(),
        }
        if self.options.token and self.options.token.strip():
            headers['Authorization'] = f'Bearer {self.options.token.strip()}'
        if self.options.route_tag and self.options.route_tag.strip():
            headers['SKRouteTag'] = self.options.route_tag
        return headers

    def _api_fetch(self, endpoint: str, body: object, timeout_ms: int, label: str) -> str:
        base_url = self._ensure_trailing_slash(self.options.base_url)
        url = f'{base_url}{endpoint}'
        json_body = json.dumps(to_dict(body), ensure_ascii=False)
        body_bytes = json_body.encode('utf-8')
        headers = self._build_headers(body_bytes)

        logger.debug("POST %s body=%s", _redact_url(url), _redact_body(json_body))

        req = Request(url, data=body_bytes, headers=headers, method='POST')
        timeout_sec = timeout_ms / 1000.0

        try:
            with urlopen(req, timeout=timeout_sec) as resp:
                raw_text = resp.read().decode('utf-8')
                logger.debug("%s status=%s raw=%s", label, resp.status, _redact_body(raw_text))
                return raw_text
        except HTTPError as e:
            raw_text = e.read().decode('utf-8') if e.fp else ''
            raise WeixinApiException(f'{label} {e.code}: {raw_text}', e.code)

    def get_updates(self, get_updates_buf: Optional[str] = None) -> GetUpdatesResponse:
        """获取更新（长轮询）"""
        request = GetUpdatesRequest(
            get_updates_buf=get_updates_buf or '',
            base_info=self._build_base_info(),
        )
        try:
            raw_text = self._api_fetch('ilink/bot/getupdates', request,
                                        self.options.long_poll_timeout_ms, 'getUpdates')
            data = json.loads(raw_text)
            resp = GetUpdatesResponse(
                ret=data.get('ret'), errcode=data.get('errcode'), errmsg=data.get('errmsg'),
                get_updates_buf=data.get('get_updates_buf'),
                longpolling_timeout_ms=data.get('longpolling_timeout_ms'),
            )
            if data.get('msgs'):
                resp.messages = [parse_weixin_message(m) for m in data['msgs']]
            return resp
        except (URLError, TimeoutError):
            logger.debug("getUpdates: client-side timeout after %dms", self.options.long_poll_timeout_ms)
            return GetUpdatesResponse(ret=0, messages=[], get_updates_buf=get_updates_buf)

    def get_upload_url(self, request: GetUploadUrlRequest) -> GetUploadUrlResponse:
        """获取上传URL"""
        request.base_info = self._build_base_info()
        raw_text = self._api_fetch('ilink/bot/getuploadurl', request,
                                    self.options.timeout_ms, 'getUploadUrl')
        data = json.loads(raw_text)
        return GetUploadUrlResponse(
            upload_param=data.get('upload_param'),
            thumb_upload_param=data.get('thumb_upload_param'),
        )

    def send_message(self, request: SendMessageRequest) -> None:
        """发送消息"""
        self._api_fetch('ilink/bot/sendmessage', request, self.options.timeout_ms, 'sendMessage')

    def get_config(self, ilink_user_id: str, context_token: Optional[str] = None) -> GetConfigResponse:
        """获取配置"""
        request = GetConfigRequest(
            ilink_user_id=ilink_user_id,
            context_token=context_token,
            base_info=self._build_base_info(),
        )
        raw_text = self._api_fetch('ilink/bot/getconfig', request,
                                    self.options.config_timeout_ms, 'getConfig')
        data = json.loads(raw_text)
        return GetConfigResponse(
            ret=data.get('ret'), errmsg=data.get('errmsg'),
            typing_ticket=data.get('typing_ticket'),
        )

    def send_typing(self, request: SendTypingRequest) -> None:
        """发送输入状态"""
        request.base_info = self._build_base_info()
        self._api_fetch('ilink/bot/sendtyping', request, self.options.config_timeout_ms, 'sendTyping')


def _redact_url(url: str) -> str:
    idx = url.find('?')
    return url[:idx] + '?[REDACTED]' if idx >= 0 else url

def _redact_body(body: str) -> str:
    if not body or len(body) < 200:
        return body
    return body[:200] + '...[TRUNCATED]'
