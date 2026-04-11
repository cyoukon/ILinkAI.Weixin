"""上传文件示例"""
import os
from samples.example_base import ExampleBase
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.media.media_upload_service import MediaUploadService
from ilinkai_weixin.weixin_api_client import DEFAULT_BASE_URL, DEFAULT_CDN_BASE_URL


class UploadExample(ExampleBase):
    name = "upload"
    description = "上传文件示例"
    usage = "upload --file <文件路径> --to <用户ID> --token <令牌>"

    def execute(self, args: list) -> int:
        self.print_header("上传文件示例")

        file_path = self.parse_argument(args, "--file")
        to_user_id = self.parse_argument(args, "--to")
        token = self.parse_argument(args, "--token")

        if not file_path or not to_user_id:
            print(f"用法: {self.usage}")
            return 1

        if not os.path.exists(file_path):
            self.print_error(f"文件不存在: {file_path}")
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
        upload_service = MediaUploadService(client, DEFAULT_CDN_BASE_URL)

        try:
            print(f"正在上传文件: {file_path}")
            uploaded = upload_service.upload_image(file_path, to_user_id)
            self.print_success("上传成功！")
            print(f"  文件键: {uploaded.file_key}")
            print(f"  下载参数: {uploaded.download_encrypted_query_param[:40]}...")
            print(f"  文件大小: {uploaded.file_size} 字节")
            print(f"  密文大小: {uploaded.file_size_ciphertext} 字节")
            return 0
        except Exception as ex:
            self.print_error(f"上传失败: {ex}")
            return 1
