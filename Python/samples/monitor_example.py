"""监控消息示例"""
import os
import tempfile
from datetime import datetime

from samples.example_base import ExampleBase
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.messaging.message_monitor_service import MessageMonitorService
from ilinkai_weixin.weixin_api_client import DEFAULT_CDN_BASE_URL


class MonitorExample(ExampleBase):
    name = "monitor"
    description = "监控消息示例"
    usage = "monitor --account-id <账户ID> --token <令牌>"

    def execute(self, args: list) -> int:
        self.print_header("监控消息示例")

        account_id = self.parse_argument(args, "--account-id")
        token = self.parse_argument(args, "--token")

        account_store = AccountStore()

        if not account_id:
            accounts = account_store.list_account_ids()
            if accounts:
                account_id = accounts[0]
                print(f"使用账户: {account_id}")

        if not account_id:
            self.print_error("未指定账户ID，请使用 --account-id 参数或先登录")
            return 1

        account = account_store.resolve_account(account_id)
        if not token:
            token = account.token

        if not token:
            self.print_error("未找到认证令牌，请先登录")
            return 1

        media_dir = os.path.join(tempfile.gettempdir(), "ilinkai-weixin", "media")

        monitor = MessageMonitorService(
            base_url=account.base_url, cdn_base_url=account.cdn_base_url,
            token=token, account_id=account.account_id, media_dir=media_dir)

        def on_message(ctx):
            print()
            print("========================================")
            print(f"收到消息 [{datetime.now().strftime('%H:%M:%S')}]")
            print(f"  发送者: {ctx.from_}")
            print(f"  内容: {ctx.body}")
            ct = ctx.context_token
            if ct and len(ct) > 20:
                ct = ct[:20] + "..."
            print(f"  上下文令牌: {ct}")
            if ctx.media_path:
                print(f"  媒体文件: {ctx.media_path}")
                print(f"  媒体类型: {ctx.media_type}")
            print("========================================")

        def on_error(err):
            self.print_error(f"错误: {err}")

        monitor.on_message_received = on_message
        monitor.on_error = on_error

        print("开始监控消息，按 Ctrl+C 停止...")
        print()

        try:
            monitor.start()
        except KeyboardInterrupt:
            print("\n监控已停止")
        return 0
