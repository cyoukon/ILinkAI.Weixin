"""获取配置示例"""
from samples.example_base import ExampleBase
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.weixin_api_client import DEFAULT_BASE_URL


class ConfigExample(ExampleBase):
    name = "config"
    description = "获取配置示例"
    usage = "config --user-id <用户ID> --token <令牌>"

    def execute(self, args: list) -> int:
        self.print_header("获取配置示例")

        user_id = self.parse_argument(args, "--user-id")
        token = self.parse_argument(args, "--token")

        if not user_id:
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

        try:
            config = client.get_config(user_id)
            print("配置信息:")
            print(f"  返回码: {config.ret}")
            print(f"  错误消息: {config.errmsg}")
            if config.typing_ticket:
                print(f"  输入票据: {config.typing_ticket[:30]}...")
            return 0
        except Exception as ex:
            self.print_error(f"获取配置失败: {ex}")
            return 1
