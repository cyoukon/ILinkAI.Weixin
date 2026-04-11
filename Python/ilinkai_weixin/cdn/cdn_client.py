"""CDN客户端服务"""
import base64
import logging
from urllib.request import Request, urlopen
from urllib.error import HTTPError

from .aes_ecb_crypto import AesEcbCrypto
from .cdn_url_builder import CdnUrlBuilder

logger = logging.getLogger(__name__)
_MAX_RETRIES = 3


class CdnUploadException(Exception):
    def __init__(self, message: str, status_code: int = 0):
        super().__init__(message)
        self.status_code = status_code


class CdnDownloadException(Exception):
    def __init__(self, message: str, status_code: int = 0):
        super().__init__(message)
        self.status_code = status_code


class CdnClient:
    """CDN客户端服务 - 处理文件的上传和下载"""

    def upload_buffer(self, buffer: bytes, upload_param: str, file_key: str,
                      cdn_base_url: str, aes_key: bytes, label: str = "upload") -> str:
        """上传缓冲区到CDN（带AES-128-ECB加密）"""
        ciphertext = AesEcbCrypto.encrypt(buffer, aes_key)
        cdn_url = CdnUrlBuilder.build_upload_url(upload_param, file_key, cdn_base_url)
        logger.debug("%s: CDN POST url=%s ciphertextSize=%d", label, _redact_url(cdn_url), len(ciphertext))

        last_error = None
        for attempt in range(1, _MAX_RETRIES + 1):
            try:
                req = Request(cdn_url, data=ciphertext, method='POST')
                req.add_header('Content-Type', 'application/octet-stream')
                with urlopen(req) as resp:
                    status = resp.status
                    if status < 200 or status >= 300:
                        raise CdnUploadException(f"CDN upload server error: status {status}", status)
                    download_param = resp.headers.get('x-encrypted-param')
                    if not download_param:
                        raise CdnUploadException("CDN upload response missing x-encrypted-param header")
                    logger.debug("%s: CDN upload success attempt=%d", label, attempt)
                    return download_param
            except HTTPError as e:
                if 400 <= e.code < 500:
                    err_msg = e.headers.get('x-error-message') or (e.read().decode('utf-8') if e.fp else '')
                    raise CdnUploadException(f"CDN upload client error {e.code}: {err_msg}", e.code)
                err_msg = e.headers.get('x-error-message') or f"status {e.code}"
                raise CdnUploadException(f"CDN upload server error: {err_msg}", e.code)
            except CdnUploadException:
                raise
            except Exception as e:
                last_error = e
                if attempt < _MAX_RETRIES:
                    logger.error("%s: attempt %d failed, retrying... err=%s", label, attempt, e)

        raise CdnUploadException(f"CDN upload failed after {_MAX_RETRIES} attempts: {last_error}")

    def download_and_decrypt(self, encrypted_query_param: str, aes_key_base64: str,
                              cdn_base_url: str, label: str = "download") -> bytes:
        """从CDN下载并解密数据"""
        key = _parse_aes_key(aes_key_base64, label)
        url = CdnUrlBuilder.build_download_url(encrypted_query_param, cdn_base_url)
        logger.debug("%s: fetching url=%s", label, _redact_url(url))
        encrypted = _fetch_cdn_bytes(url, label)
        logger.debug("%s: downloaded %d bytes, decrypting", label, len(encrypted))
        decrypted = AesEcbCrypto.decrypt(encrypted, key)
        logger.debug("%s: decrypted %d bytes", label, len(decrypted))
        return decrypted

    def download_plain(self, encrypted_query_param: str, cdn_base_url: str,
                        label: str = "download") -> bytes:
        """从CDN下载原始数据（不解密）"""
        url = CdnUrlBuilder.build_download_url(encrypted_query_param, cdn_base_url)
        logger.debug("%s: fetching url=%s", label, _redact_url(url))
        return _fetch_cdn_bytes(url, label)


def _fetch_cdn_bytes(url: str, label: str) -> bytes:
    try:
        with urlopen(url) as resp:
            if resp.status < 200 or resp.status >= 300:
                body = resp.read().decode('utf-8', errors='replace')
                raise CdnDownloadException(f"{label}: CDN download {resp.status} body={body}", resp.status)
            return resp.read()
    except CdnDownloadException:
        raise
    except HTTPError as e:
        body = e.read().decode('utf-8', errors='replace') if e.fp else ''
        raise CdnDownloadException(f"{label}: CDN download {e.code} body={body}", e.code)
    except Exception as e:
        raise CdnDownloadException(f"{label}: fetch network error: {e}")


def _parse_aes_key(aes_key_base64: str, label: str) -> bytes:
    decoded = base64.b64decode(aes_key_base64)
    if len(decoded) == 16:
        return decoded
    if len(decoded) == 32:
        hex_string = decoded.decode('ascii', errors='replace')
        if all(c in '0123456789abcdefABCDEF' for c in hex_string):
            return bytes.fromhex(hex_string)
    raise ValueError(f"{label}: aes_key must decode to 16 raw bytes or 32-char hex string, got {len(decoded)} bytes")


def _redact_url(url: str) -> str:
    idx = url.find('?')
    return url[:idx] + '?[REDACTED]' if idx >= 0 else url
