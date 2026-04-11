"""媒体上传服务"""
import hashlib
import logging
import os
from dataclasses import dataclass

from ..cdn.aes_ecb_crypto import AesEcbCrypto
from ..cdn.cdn_client import CdnClient
from ..models import GetUploadUrlRequest, UploadMediaType

logger = logging.getLogger(__name__)


@dataclass
class UploadedFileInfo:
    file_key: str = ""
    download_encrypted_query_param: str = ""
    aes_key: str = ""
    file_size: int = 0
    file_size_ciphertext: int = 0


class MediaUploadService:
    def __init__(self, api_client, cdn_base_url: str):
        self._api_client = api_client
        self._cdn_base_url = cdn_base_url
        self._cdn_client = CdnClient()

    def upload_file(self, file_path: str, to_user_id: str, media_type: int) -> UploadedFileInfo:
        with open(file_path, 'rb') as f:
            plaintext = f.read()
        raw_size = len(plaintext)
        raw_file_md5 = hashlib.md5(plaintext).hexdigest()
        file_size = AesEcbCrypto.get_padded_size(raw_size)
        file_key = os.urandom(16).hex()
        aes_key = AesEcbCrypto.generate_key()

        logger.debug("UploadFile: file=%s rawSize=%d fileSize=%d md5=%s fileKey=%s",
                      file_path, raw_size, file_size, raw_file_md5, file_key)

        request = GetUploadUrlRequest(
            file_key=file_key, media_type=media_type, to_user_id=to_user_id,
            raw_size=raw_size, raw_file_md5=raw_file_md5, file_size=file_size,
            no_need_thumb=True, aes_key=aes_key.hex(),
        )
        upload_url_resp = self._api_client.get_upload_url(request)
        upload_param = upload_url_resp.upload_param
        if not upload_param:
            raise RuntimeError("getUploadUrl returned no upload_param")

        download_param = self._cdn_client.upload_buffer(
            plaintext, upload_param, file_key, self._cdn_base_url, aes_key,
            f"UploadFile[fileKey={file_key}]")

        return UploadedFileInfo(
            file_key=file_key, download_encrypted_query_param=download_param,
            aes_key=aes_key.hex(), file_size=raw_size, file_size_ciphertext=file_size,
        )

    def upload_image(self, file_path: str, to_user_id: str) -> UploadedFileInfo:
        return self.upload_file(file_path, to_user_id, UploadMediaType.IMAGE)

    def upload_video(self, file_path: str, to_user_id: str) -> UploadedFileInfo:
        return self.upload_file(file_path, to_user_id, UploadMediaType.VIDEO)

    def upload_file_attachment(self, file_path: str, to_user_id: str) -> UploadedFileInfo:
        return self.upload_file(file_path, to_user_id, UploadMediaType.FILE)
