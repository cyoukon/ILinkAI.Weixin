"""消息监控服务"""
import json
import logging
import math
import os
import time
from typing import Optional, Callable

from ..weixin_api_client import WeixinApiClient, WeixinApiClientOptions
from ..auth.session_guard import SessionGuard, WeixinSessionPausedException, SESSION_EXPIRED_ERROR_CODE
from ..auth.account_store import AccountStore
from .message_processor import MessageProcessor, MessageContext

logger = logging.getLogger(__name__)

_MAX_CONSECUTIVE_FAILURES = 3
_BACKOFF_DELAY_S = 30
_RETRY_DELAY_S = 2


class MessageMonitorService:
    """消息监控服务 - 长轮询获取微信消息"""

    def __init__(self, base_url: str, cdn_base_url: str, token: str,
                 account_id: str, long_poll_timeout_ms: int = 35000,
                 media_dir: Optional[str] = None):
        self._base_url = base_url
        self._cdn_base_url = cdn_base_url
        self._account_id = account_id
        self._media_dir = media_dir
        self._api_client = WeixinApiClient(WeixinApiClientOptions(
            base_url=base_url, cdn_base_url=cdn_base_url,
            token=token, long_poll_timeout_ms=long_poll_timeout_ms,
        ))
        self._session_guard = SessionGuard()
        self._message_processor = MessageProcessor(cdn_base_url)
        self._running = False

        state_dir = self._get_state_dir()
        os.makedirs(state_dir, exist_ok=True)
        normalized = AccountStore.normalize_account_id(account_id)
        self._sync_buf_file = os.path.join(state_dir, f"{normalized}_sync_buf.json")

        self.on_message_received: Optional[Callable[[MessageContext], None]] = None
        self.on_error: Optional[Callable[[Exception], None]] = None

    @staticmethod
    def _get_state_dir() -> str:
        env_path = os.environ.get('ILINKAI_STATE_DIR')
        if env_path:
            return env_path
        return os.path.join(os.path.expanduser('~'), '.ilinkai', 'sync')

    def _load_sync_buf(self) -> str:
        try:
            if not os.path.exists(self._sync_buf_file):
                return ""
            with open(self._sync_buf_file, 'r') as f:
                data = json.load(f)
            return data.get('get_updates_buf', '')
        except Exception:
            return ""

    def _save_sync_buf(self, sync_buf: str):
        try:
            with open(self._sync_buf_file, 'w') as f:
                json.dump({'get_updates_buf': sync_buf}, f)
        except Exception as e:
            logger.error("Failed to save sync buf: %s", e)

    def start(self):
        """启动监控（阻塞）"""
        self._running = True
        logger.info("Monitor started: baseUrl=%s accountId=%s", self._base_url, self._account_id)

        sync_buf = self._load_sync_buf()
        consecutive_failures = 0

        while self._running:
            try:
                self._session_guard.assert_session_active(self._account_id)
                response = self._api_client.get_updates(sync_buf)

                is_api_error = (response.ret is not None and response.ret != 0) or \
                               (response.errcode is not None and response.errcode != 0)

                if is_api_error:
                    is_session_expired = (response.errcode == SESSION_EXPIRED_ERROR_CODE) or \
                                         (response.ret == SESSION_EXPIRED_ERROR_CODE)
                    if is_session_expired:
                        self._session_guard.pause_session(self._account_id)
                        pause_ms = self._session_guard.get_remaining_pause_ms(self._account_id)
                        logger.error("getUpdates: session expired, pausing for %d min",
                                     math.ceil(pause_ms / 60000))
                        if self.on_error:
                            self.on_error(WeixinSessionPausedException(
                                self._account_id, int(math.ceil(pause_ms / 60000))))
                        consecutive_failures = 0
                        time.sleep(pause_ms / 1000)
                        continue

                    consecutive_failures += 1
                    if consecutive_failures >= _MAX_CONSECUTIVE_FAILURES:
                        consecutive_failures = 0
                        time.sleep(_BACKOFF_DELAY_S)
                    else:
                        time.sleep(_RETRY_DELAY_S)
                    continue

                consecutive_failures = 0
                if response.get_updates_buf:
                    self._save_sync_buf(response.get_updates_buf)
                    sync_buf = response.get_updates_buf

                messages = response.messages or []
                for msg in messages:
                    context = self._message_processor.convert_to_context(msg, self._account_id)
                    if self._media_dir:
                        self._message_processor.process_media(context, self._media_dir)
                    if self.on_message_received:
                        self.on_message_received(context)

            except WeixinSessionPausedException:
                raise
            except KeyboardInterrupt:
                break
            except Exception as err:
                consecutive_failures += 1
                logger.error("getUpdates error (%d/%d): %s",
                             consecutive_failures, _MAX_CONSECUTIVE_FAILURES, err)
                if self.on_error:
                    self.on_error(err)
                if consecutive_failures >= _MAX_CONSECUTIVE_FAILURES:
                    consecutive_failures = 0
                    time.sleep(_BACKOFF_DELAY_S)
                else:
                    time.sleep(_RETRY_DELAY_S)

        logger.info("Monitor ended")

    def stop(self):
        self._running = False
