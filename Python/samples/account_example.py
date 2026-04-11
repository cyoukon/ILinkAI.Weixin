"""账户管理示例"""
from samples.example_base import ExampleBase
from ilinkai_weixin.auth.account_store import AccountStore


class AccountExample(ExampleBase):
    name = "account"
    description = "账户管理示例"
    usage = "account list"

    def execute(self, args: list) -> int:
        self.print_header("账户管理示例")

        account_store = AccountStore()

        if len(args) < 2 or args[1] != "list":
            print(f"用法: {self.usage}")
            return 1

        accounts = account_store.list_account_ids()

        if not accounts:
            print("没有已注册的账户")
            print("请先运行 'login' 命令进行登录")
            return 0

        print(f"已注册账户 ({len(accounts)} 个):")
        print()

        for id_ in accounts:
            account = account_store.resolve_account(id_)
            account_data = account_store.load_account(id_)

            print(f"账户ID: {account.account_id}")
            print(f"  基础URL: {account.base_url}")
            print(f"  CDN URL: {account.cdn_base_url}")
            print(f"  已配置: {account.configured}")
            print(f"  已启用: {account.enabled}")

            if account_data and account_data.token:
                print(f"  Token: {account_data.token[:20]}...")
            if account_data and account_data.user_id:
                print(f"  用户ID: {account_data.user_id}")
            if account_data and account_data.saved_at:
                print(f"  保存时间: {account_data.saved_at}")
            print()

        return 0
