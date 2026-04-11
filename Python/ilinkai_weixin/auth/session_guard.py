"""会话守卫服务"""
import math
import time


class WeixinSessionPausedException(Exception):
    def __init__(self, account_id: str, remaining_minutes: int):
        super().__init__(
            f"Session paused for accountId={account_id}, "
            f"{remaining_minutes} min remaining (errcode -14)"
        )
        self.account_id = account_id
        self.remaining_minutes = remaining_minutes


SESSION_EXPIRED_ERROR_CODE = -14


class SessionGuard:
    """会话守卫 - 管理会话状态和暂停逻辑"""
    _SESSION_PAUSE_DURATION_MS = 60 * 60 * 1000

    def __init__(self):
        self._pause_until_map: dict[str, int] = {}

    def pause_session(self, account_id: str):
        until = int(time.time() * 1000) + self._SESSION_PAUSE_DURATION_MS
        self._pause_until_map[account_id] = until

    def is_session_paused(self, account_id: str) -> bool:
        until = self._pause_until_map.get(account_id)
        if until is None:
            return False
        if int(time.time() * 1000) >= until:
            del self._pause_until_map[account_id]
            return False
        return True

    def get_remaining_pause_ms(self, account_id: str) -> int:
        until = self._pause_until_map.get(account_id)
        if until is None:
            return 0
        remaining = until - int(time.time() * 1000)
        if remaining <= 0:
            del self._pause_until_map[account_id]
            return 0
        return remaining

    def assert_session_active(self, account_id: str):
        if self.is_session_paused(account_id):
            remaining_min = math.ceil(self.get_remaining_pause_ms(account_id) / 60000)
            raise WeixinSessionPausedException(account_id, remaining_min)

    def reset_for_test(self):
        self._pause_until_map.clear()
