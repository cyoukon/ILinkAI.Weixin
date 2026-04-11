"""媒体下载服务"""
import logging
import os
import uuid
from datetime import datetime

from ..cdn.cdn_client import CdnClient

logger = logging.getLogger(__name__)
MAX_MEDIA_BYTES = 100 * 1024 * 1024


class MediaDownloadService:
    def __init__(self, cdn_base_url: str):
        self._cdn_base_url = cdn_base_url
        self._cdn_client = CdnClient()

    def download_image(self, encrypt_query_param: str, aes_key_base64: str = None) -> bytes:
        if not encrypt_query_param:
            raise ValueError("encrypt_query_param is required")
        if aes_key_base64:
            return self._cdn_client.download_and_decrypt(
                encrypt_query_param, aes_key_base64, self._cdn_base_url, "DownloadImage")
        return self._cdn_client.download_plain(
            encrypt_query_param, self._cdn_base_url, "DownloadImage-plain")

    def download_voice(self, encrypt_query_param: str, aes_key_base64: str) -> bytes:
        if not encrypt_query_param or not aes_key_base64:
            raise ValueError("encrypt_query_param and aes_key_base64 are required")
        return self._cdn_client.download_and_decrypt(
            encrypt_query_param, aes_key_base64, self._cdn_base_url, "DownloadVoice")

    def download_file(self, encrypt_query_param: str, aes_key_base64: str) -> bytes:
        if not encrypt_query_param or not aes_key_base64:
            raise ValueError("encrypt_query_param and aes_key_base64 are required")
        return self._cdn_client.download_and_decrypt(
            encrypt_query_param, aes_key_base64, self._cdn_base_url, "DownloadFile")

    def download_video(self, encrypt_query_param: str, aes_key_base64: str) -> bytes:
        if not encrypt_query_param or not aes_key_base64:
            raise ValueError("encrypt_query_param and aes_key_base64 are required")
        return self._cdn_client.download_and_decrypt(
            encrypt_query_param, aes_key_base64, self._cdn_base_url, "DownloadVideo")

    @staticmethod
    def save_media_to_file(data: bytes, directory: str, file_name: str = None) -> str:
        os.makedirs(directory, exist_ok=True)
        if not file_name:
            file_name = f"media_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}_{uuid.uuid4().hex}.bin"
        file_path = os.path.join(directory, file_name)
        with open(file_path, 'wb') as f:
            f.write(data)
        logger.debug("SaveMediaToFile: saved to %s", file_path)
        return file_path
