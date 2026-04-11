"""CDN URL构建工具"""
from urllib.parse import quote


class CdnUrlBuilder:
    @staticmethod
    def build_download_url(encrypted_query_param: str, cdn_base_url: str) -> str:
        base_url = cdn_base_url if cdn_base_url.endswith('/') else cdn_base_url + '/'
        return f'{base_url}download?encrypted_query_param={quote(encrypted_query_param)}'

    @staticmethod
    def build_upload_url(upload_param: str, file_key: str, cdn_base_url: str) -> str:
        base_url = cdn_base_url if cdn_base_url.endswith('/') else cdn_base_url + '/'
        return f'{base_url}upload?encrypted_query_param={quote(upload_param)}&filekey={quote(file_key)}'
