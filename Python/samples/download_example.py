"""下载文件示例"""
import os
import tempfile
from samples.example_base import ExampleBase
from ilinkai_weixin.media.media_download_service import MediaDownloadService
from ilinkai_weixin.weixin_api_client import DEFAULT_CDN_BASE_URL


class DownloadExample(ExampleBase):
    name = "download"
    description = "下载文件示例"
    usage = "download --param <加密参数> --aes-key <AES密钥> --output <输出路径>"

    def execute(self, args: list) -> int:
        self.print_header("下载文件示例")

        encrypt_param = self.parse_argument(args, "--param")
        aes_key = self.parse_argument(args, "--aes-key")
        output = self.parse_argument(args, "--output")

        if not encrypt_param:
            print(f"用法: {self.usage}")
            return 1

        if not output:
            output = os.path.join(tempfile.gettempdir(), "ilinkai-weixin", "downloaded_media.bin")

        download_service = MediaDownloadService(DEFAULT_CDN_BASE_URL)

        try:
            print("正在下载...")
            data = download_service.download_image(encrypt_param, aes_key)
            download_service.save_media_to_file(data, os.path.dirname(output), os.path.basename(output))
            self.print_success("下载成功！")
            print(f"  保存路径: {output}")
            print(f"  文件大小: {len(data)} 字节")
            return 0
        except Exception as ex:
            self.print_error(f"下载失败: {ex}")
            return 1
