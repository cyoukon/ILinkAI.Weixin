"""扫码登录示例"""
import sys
sys.path.insert(0, '.')

from samples.example_base import ExampleBase
from samples.helpers import display_qrcode_with_border
from ilinkai_weixin.auth.qrcode_login_service import QRCodeLoginService
from ilinkai_weixin.auth.account_store import AccountStore, WeixinAccountData
from ilinkai_weixin.weixin_api_client import DEFAULT_BASE_URL


class LoginExample(ExampleBase):
    name = "login"
    description = "扫码登录示例"
    usage = "login"

    def execute(self, args: list) -> int:
        self.print_header("扫码登录示例")

        login_service = QRCodeLoginService(DEFAULT_BASE_URL)

        print("正在获取登录二维码...")
        start_result = login_service.start_login()

        if not start_result.qr_code_url:
            self.print_error(f"获取二维码失败: {start_result.message}")
            return 1

        print()
        print("请使用微信扫描以下二维码登录:")
        display_qrcode_with_border(start_result.qr_code_url, "微信扫码登录")
        print("等待扫码...")

        def on_status(status):
            if status == ".":
                print(".", end="", flush=True)
            else:
                print(status)

        def on_qr_refreshed(url):
            print()
            print("二维码已刷新:")
            display_qrcode_with_border(url, "微信扫码登录 (已刷新)")

        wait_result = login_service.wait_for_login(
            start_result.qr_code, timeout_ms=300000,
            on_status_changed=on_status, on_qr_refreshed=on_qr_refreshed)

        if wait_result.connected:
            print()
            print("========================================")
            self.print_success("登录成功！")
            print(f"账户ID: {wait_result.account_id}")
            print(f"用户ID: {wait_result.user_id}")
            if wait_result.bot_token:
                print(f"Token: {wait_result.bot_token[:30]}...")
            print("========================================")

            account_store = AccountStore()
            normalized_id = AccountStore.normalize_account_id(wait_result.account_id or "")
            account_store.save_account(normalized_id, WeixinAccountData(
                token=wait_result.bot_token,
                base_url=wait_result.base_url,
                user_id=wait_result.user_id,
            ))
            account_store.register_account_id(normalized_id)
            print(f"账户已保存: {normalized_id}")
            return 0
        else:
            print()
            self.print_error(f"登录失败: {wait_result.message}")
            return 1
