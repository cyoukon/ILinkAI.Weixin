"""发送输入状态示例"""
import time
from samples.example_base import ExampleBase
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.messaging.message_send_service import MessageSendService
from ilinkai_weixin.weixin_api_client import DEFAULT_BASE_URL, DEFAULT_CDN_BASE_URL


class TypingExample(ExampleBase):
    name = "typing"
    description = "发送输入状态示例"
    usage = "typing --user-id <用户ID> --ticket <输入票据> --token <令牌>"

    def execute(self, args: list) -> int:
        self.print_header("发送输入状态示例")

        user_id = self.parse_argument(args, "--user-id")
        token = self.parse_argument(args, "--token")
        typing_ticket = self.parse_argument(args, "--ticket")

        if not user_id or not typing_ticket:
            print(f"用法: {self.usage}")
            return 1

        account_store = AccountStore()
        accounts = account_store.list_account_ids()
        if not token and accounts:
            account = account_store.load_account(accounts[0])
            if account: token = account.token
        if not token:
            self.print_error("未找到认证令牌")
            return 1

        client = WeixinApiClient(WeixinApiClientOptions(base_url=DEFAULT_BASE_URL, token=token))
        send_service = MessageSendService(client, DEFAULT_CDN_BASE_URL)

        try:
            send_service.send_typing(user_id, typing_ticket, True)
            self.print_success("输入状态已发送")
            time.sleep(3)
            send_service.send_typing(user_id, typing_ticket, False)
            self.print_success("已取消输入状态")
            return 0
        except Exception as ex:
            self.print_error(f"发送失败: {ex}")
            return 1
