"""二维码登录服务"""
import json
import logging
import time
import uuid
from dataclasses import dataclass
from typing import Optional, Callable
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from urllib.parse import quote

from ..weixin_api_client import WeixinApiException
from ..models import GetQRCodeResponse, GetQRCodeStatusResponse

logger = logging.getLogger(__name__)

DEFAULT_ILINK_BOT_TYPE = "3"
_QR_LONG_POLL_TIMEOUT_MS = 35_000
_MAX_QR_REFRESH_COUNT = 3


@dataclass
class QRLoginStartResult:
    session_key: str = ""
    qr_code: str = ""
    qr_code_url: Optional[str] = None
    message: str = ""


@dataclass
class QRLoginWaitResult:
    connected: bool = False
    bot_token: Optional[str] = None
    account_id: Optional[str] = None
    base_url: Optional[str] = None
    user_id: Optional[str] = None
    message: str = ""


class QRCodeLoginService:
    """二维码登录服务"""

    def __init__(self, api_base_url: str, route_tag: Optional[str] = None):
        self._api_base_url = api_base_url or "https://ilinkai.weixin.qq.com"
        self._route_tag = route_tag

    def fetch_qr_code(self, bot_type: str = DEFAULT_ILINK_BOT_TYPE) -> GetQRCodeResponse:
        base_url = self._api_base_url if self._api_base_url.endswith('/') else self._api_base_url + '/'
        url = f'{base_url}ilink/bot/get_bot_qrcode?bot_type={quote(bot_type)}'
        logger.info("Fetching QR code from: %s", url)

        req = Request(url, method='GET')
        if self._route_tag:
            req.add_header('SKRouteTag', self._route_tag)

        try:
            with urlopen(req, timeout=_QR_LONG_POLL_TIMEOUT_MS / 1000) as resp:
                data = json.loads(resp.read().decode('utf-8'))
                return GetQRCodeResponse(
                    qrcode=data.get('qrcode'),
                    qrcode_img_content=data.get('qrcode_img_content'),
                )
        except HTTPError as e:
            body = e.read().decode('utf-8') if e.fp else ''
            raise WeixinApiException(f'Failed to fetch QR code: {e.code} {body}', e.code)

    def poll_qr_status(self, qrcode: str) -> GetQRCodeStatusResponse:
        base_url = self._api_base_url if self._api_base_url.endswith('/') else self._api_base_url + '/'
        url = f'{base_url}ilink/bot/get_qrcode_status?qrcode={quote(qrcode)}'

        req = Request(url, method='GET')
        req.add_header('iLink-App-ClientVersion', '1')
        if self._route_tag:
            req.add_header('SKRouteTag', self._route_tag)

        try:
            with urlopen(req, timeout=_QR_LONG_POLL_TIMEOUT_MS / 1000) as resp:
                data = json.loads(resp.read().decode('utf-8'))
                return GetQRCodeStatusResponse(
                    status=data.get('status'), bot_token=data.get('bot_token'),
                    ilink_bot_id=data.get('ilink_bot_id'), baseurl=data.get('baseurl'),
                    ilink_user_id=data.get('ilink_user_id'),
                )
        except (TimeoutError, OSError):
            logger.debug("pollQRStatus: client-side timeout, returning wait")
            return GetQRCodeStatusResponse(status='wait')
        except HTTPError as e:
            raise WeixinApiException(f'Failed to poll QR status: {e.code}', e.code)

    def start_login(self, account_id: Optional[str] = None,
                    bot_type: Optional[str] = None) -> QRLoginStartResult:
        session_key = account_id or str(uuid.uuid4())
        bot_type = bot_type or DEFAULT_ILINK_BOT_TYPE
        logger.info("Starting Weixin login with bot_type=%s", bot_type)
        try:
            qr_response = self.fetch_qr_code(bot_type)
            return QRLoginStartResult(
                session_key=session_key,
                qr_code=qr_response.qrcode or '',
                qr_code_url=qr_response.qrcode_img_content,
                message='使用微信扫描以下二维码，以完成连接。',
            )
        except Exception as err:
            logger.error("Failed to start Weixin login: %s", err)
            return QRLoginStartResult(session_key=session_key, message=f'Failed to start login: {err}')

    def wait_for_login(self, qrcode: str, timeout_ms: int = 480_000,
                       on_status_changed: Optional[Callable[[str], None]] = None,
                       on_qr_refreshed: Optional[Callable[[str], None]] = None) -> QRLoginWaitResult:
        deadline = time.time() + timeout_ms / 1000
        qr_refresh_count = 1
        current_qr_code = qrcode
        scanned_printed = False

        while time.time() < deadline:
            try:
                status_response = self.poll_qr_status(current_qr_code)
                status = (status_response.status or '').lower()

                if status == 'wait':
                    if on_status_changed: on_status_changed('.')
                elif status == 'scaned':
                    if not scanned_printed:
                        if on_status_changed: on_status_changed('\n👀 已扫码，在微信继续操作...\n')
                        scanned_printed = True
                elif status == 'expired':
                    qr_refresh_count += 1
                    if qr_refresh_count > _MAX_QR_REFRESH_COUNT:
                        return QRLoginWaitResult(connected=False, message='登录超时：二维码多次过期，请重新开始登录流程。')
                    if on_status_changed:
                        on_status_changed(f'\n⏳ 二维码已过期，正在刷新...({qr_refresh_count}/{_MAX_QR_REFRESH_COUNT})\n')
                    try:
                        new_qr = self.fetch_qr_code()
                        current_qr_code = new_qr.qrcode or ''
                        scanned_printed = False
                        if on_qr_refreshed: on_qr_refreshed(new_qr.qrcode_img_content or '')
                    except Exception as refresh_err:
                        return QRLoginWaitResult(connected=False, message=f'刷新二维码失败: {refresh_err}')
                elif status == 'confirmed':
                    if not status_response.ilink_bot_id:
                        return QRLoginWaitResult(connected=False, message='登录失败：服务器未返回 ilink_bot_id。')
                    logger.info("✅ Login confirmed! ilink_bot_id=%s", status_response.ilink_bot_id)
                    return QRLoginWaitResult(
                        connected=True, bot_token=status_response.bot_token,
                        account_id=status_response.ilink_bot_id, base_url=status_response.baseurl,
                        user_id=status_response.ilink_user_id, message='✅ 与微信连接成功！',
                    )
            except Exception as err:
                logger.error("Error polling QR status: %s", err)
                return QRLoginWaitResult(connected=False, message=f'Login failed: {err}')
            time.sleep(1)

        return QRLoginWaitResult(connected=False, message='登录超时，请重试。')
