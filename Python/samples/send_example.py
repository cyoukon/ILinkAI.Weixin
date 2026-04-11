"""发送消息示例"""
from samples.example_base import ExampleBase
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.messaging.message_send_service import MessageSendService
from ilinkai_weixin.weixin_api_client import DEFAULT_BASE_URL, DEFAULT_CDN_BASE_URL


class SendExample(ExampleBase):
    name = "send"
    description = "发送消息示例"
    usage = "send --to <用户ID> --text <消息内容> --token <令牌> --context-token <上下文令牌>"

    def execute(self, args: list) -> int:
        self.print_header("发送消息示例")

        to = self.parse_argument(args, "--to")
        text = self.parse_argument(args, "--text")
        token = self.parse_argument(args, "--token")
        context_token = self.parse_argument(args, "--context-token")

        if not to or not text:
            print(f"用法: {self.usage}")
            return 1

        account_store = AccountStore()
        accounts = account_store.list_account_ids()

        if not token and accounts:
            account = account_store.load_account(accounts[0])
            if account:
                token = account.token
            print(f"使用已保存的账户: {accounts[0]}")

        if not token:
            self.print_error("未找到认证令牌，请先登录或使用 --token 参数")
            return 1

        if not context_token:
            self.print_warning("未提供 context-token，消息可能无法正确关联到会话")
            context_token = ""

        client = WeixinApiClient(WeixinApiClientOptions(base_url=DEFAULT_BASE_URL, token=token))
        send_service = MessageSendService(client, DEFAULT_CDN_BASE_URL)

        try:
            message_id = send_service.send_text(to, text, context_token)
            self.print_success(f"消息发送成功！ID: {message_id}")
            return 0
        except Exception as ex:
            self.print_error(f"发送失败: {ex}")
            return 1
