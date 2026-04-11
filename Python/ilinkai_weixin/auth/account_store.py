"""账户存储服务"""
import json
import logging
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Optional, List

logger = logging.getLogger(__name__)

DEFAULT_BASE_URL = "https://ilinkai.weixin.qq.com"
DEFAULT_CDN_BASE_URL = "https://novac2c.cdn.weixin.qq.com/c2c"


@dataclass
class WeixinAccountData:
    token: Optional[str] = None
    saved_at: Optional[str] = None
    base_url: Optional[str] = None
    user_id: Optional[str] = None


@dataclass
class ResolvedWeixinAccount:
    account_id: str = ""
    base_url: str = DEFAULT_BASE_URL
    cdn_base_url: str = DEFAULT_CDN_BASE_URL
    token: Optional[str] = None
    enabled: bool = True
    configured: bool = False
    name: Optional[str] = None


class AccountStore:
    """账户存储服务 - 管理微信账户的持久化存储"""

    def __init__(self, state_dir: Optional[str] = None):
        self._state_dir = state_dir or self._get_default_state_dir()

    @staticmethod
    def _get_default_state_dir() -> str:
        env_path = os.environ.get('ILINKAI_STATE_DIR')
        if env_path:
            return env_path
        return os.path.join(os.path.expanduser('~'), '.ilinkai')

    def _get_weixin_state_dir(self) -> str:
        return os.path.join(self._state_dir, 'weixin')

    def _get_account_index_path(self) -> str:
        return os.path.join(self._get_weixin_state_dir(), 'accounts.json')

    def _get_accounts_dir(self) -> str:
        return os.path.join(self._get_weixin_state_dir(), 'accounts')

    def _get_account_file_path(self, account_id: str) -> str:
        return os.path.join(self._get_accounts_dir(), f'{self._sanitize_account_id(account_id)}.json')

    @staticmethod
    def _sanitize_account_id(account_id: str) -> str:
        safe = account_id.strip().lower()
        for c in ['/', '\\', ':', '*', '?', '"', '<', '>', '|']:
            safe = safe.replace(c, '_')
        return safe.replace('..', '_')

    @staticmethod
    def normalize_account_id(raw_id: str) -> str:
        trimmed = raw_id.strip().lower()
        if trimmed.endswith('@im.bot'):
            return trimmed.replace('@im.bot', '-im-bot')
        if trimmed.endswith('@im.wechat'):
            return trimmed.replace('@im.wechat', '-im-wechat')
        return trimmed

    @staticmethod
    def derive_raw_account_id(normalized_id: str) -> Optional[str]:
        if normalized_id.endswith('-im-bot'):
            return normalized_id[:-7] + '@im.bot'
        if normalized_id.endswith('-im-wechat'):
            return normalized_id[:-10] + '@im.wechat'
        return None

    def list_account_ids(self) -> List[str]:
        file_path = self._get_account_index_path()
        try:
            if not os.path.exists(file_path):
                return []
            with open(file_path, 'r', encoding='utf-8') as f:
                parsed = json.load(f)
            return [id_ for id_ in (parsed or []) if id_ and id_.strip()]
        except Exception:
            return []

    def register_account_id(self, account_id: str):
        os.makedirs(self._get_weixin_state_dir(), exist_ok=True)
        existing = self.list_account_ids()
        if account_id in existing:
            return
        existing.append(account_id)
        with open(self._get_account_index_path(), 'w', encoding='utf-8') as f:
            json.dump(existing, f, indent=2)

    def load_account(self, account_id: str) -> Optional[WeixinAccountData]:
        file_path = self._get_account_file_path(account_id)
        if os.path.exists(file_path):
            return self._read_account_file(file_path)
        raw_id = self.derive_raw_account_id(account_id)
        if raw_id:
            compat_path = self._get_account_file_path(raw_id)
            if os.path.exists(compat_path):
                return self._read_account_file(compat_path)
        return None

    @staticmethod
    def _read_account_file(file_path: str) -> Optional[WeixinAccountData]:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            return WeixinAccountData(
                token=data.get('token'), saved_at=data.get('saved_at'),
                base_url=data.get('base_url'), user_id=data.get('user_id'),
            )
        except Exception:
            return None

    def save_account(self, account_id: str, data: WeixinAccountData):
        os.makedirs(self._get_accounts_dir(), exist_ok=True)
        existing = self.load_account(account_id) or WeixinAccountData()
        merged = WeixinAccountData(
            token=(data.token.strip() if data.token else None) or existing.token,
            base_url=(data.base_url.strip() if data.base_url else None) or existing.base_url,
            user_id=data.user_id or existing.user_id,
            saved_at=datetime.now(timezone.utc).isoformat(),
        )
        file_path = self._get_account_file_path(account_id)
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump({
                'token': merged.token, 'saved_at': merged.saved_at,
                'base_url': merged.base_url, 'user_id': merged.user_id,
            }, f, indent=2)

    def clear_account(self, account_id: str):
        try:
            file_path = self._get_account_file_path(account_id)
            if os.path.exists(file_path):
                os.remove(file_path)
        except Exception:
            pass

    def resolve_account(self, account_id: str) -> ResolvedWeixinAccount:
        if not account_id or not account_id.strip():
            raise ValueError("accountId is required")
        id_ = self.normalize_account_id(account_id)
        account_data = self.load_account(id_)
        return ResolvedWeixinAccount(
            account_id=id_,
            base_url=(account_data.base_url.strip() if account_data and account_data.base_url else DEFAULT_BASE_URL),
            cdn_base_url=DEFAULT_CDN_BASE_URL,
            token=(account_data.token.strip() if account_data and account_data.token else None),
            enabled=True,
            configured=bool(account_data and account_data.token and account_data.token.strip()),
        )
