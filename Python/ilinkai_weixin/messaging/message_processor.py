"""消息处理服务"""
import base64
import logging
import os
import uuid
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional, List

from ..media.media_download_service import MediaDownloadService
from ..models import WeixinMessage, MessageItem, MessageItemType

logger = logging.getLogger(__name__)


@dataclass
class MessageContext:
    body: str = ""
    from_: str = ""
    to: str = ""
    account_id: str = ""
    message_id: str = ""
    timestamp: Optional[int] = None
    context_token: Optional[str] = None
    media_path: Optional[str] = None
    media_type: Optional[str] = None
    original_message: Optional[WeixinMessage] = None


class MessageProcessor:
    def __init__(self, cdn_base_url: str):
        self._download_service = MediaDownloadService(cdn_base_url)

    def convert_to_context(self, message: WeixinMessage, account_id: str) -> MessageContext:
        from_user_id = message.from_user_id or ""
        body = extract_body_from_item_list(message.item_list)
        return MessageContext(
            body=body, from_=from_user_id, to=from_user_id,
            account_id=account_id,
            message_id=f"ilinkai-{uuid.uuid4().hex}",
            timestamp=message.create_time_ms,
            context_token=message.context_token,
            original_message=message,
        )

    def process_media(self, context: MessageContext, media_dir: str):
        message = context.original_message
        if not message or not message.item_list:
            return
        for item in message.item_list:
            if item.type is None:
                continue
            try:
                if item.type == MessageItemType.IMAGE:
                    self._process_image_item(context, item, media_dir)
                elif item.type == MessageItemType.VOICE:
                    self._process_voice_item(context, item, media_dir)
                elif item.type == MessageItemType.FILE:
                    self._process_file_item(context, item, media_dir)
                elif item.type == MessageItemType.VIDEO:
                    self._process_video_item(context, item, media_dir)
                if context.media_path:
                    break
            except Exception as err:
                logger.error("ProcessMedia: failed to process item type=%s: %s", item.type, err)

    def _process_image_item(self, ctx: MessageContext, item: MessageItem, media_dir: str):
        img = item.image_item
        if not img or not img.media or not img.media.encrypt_query_param:
            return
        aes_key_base64 = None
        if img.aes_key:
            aes_key_base64 = base64.b64encode(bytes.fromhex(img.aes_key)).decode()
        elif img.media.aes_key:
            aes_key_base64 = img.media.aes_key
        data = self._download_service.download_image(img.media.encrypt_query_param, aes_key_base64)
        ts = datetime.utcnow().strftime('%Y%m%d_%H%M%S')
        ctx.media_path = self._download_service.save_media_to_file(data, media_dir, f"image_{ts}.jpg")
        ctx.media_type = "image/*"

    def _process_voice_item(self, ctx: MessageContext, item: MessageItem, media_dir: str):
        voice = item.voice_item
        if not voice or not voice.media or not voice.media.encrypt_query_param or not voice.media.aes_key:
            return
        data = self._download_service.download_voice(voice.media.encrypt_query_param, voice.media.aes_key)
        ts = datetime.utcnow().strftime('%Y%m%d_%H%M%S')
        ctx.media_path = self._download_service.save_media_to_file(data, media_dir, f"voice_{ts}.silk")
        ctx.media_type = "audio/silk"

    def _process_file_item(self, ctx: MessageContext, item: MessageItem, media_dir: str):
        fi = item.file_item
        if not fi or not fi.media or not fi.media.encrypt_query_param or not fi.media.aes_key:
            return
        data = self._download_service.download_file(fi.media.encrypt_query_param, fi.media.aes_key)
        file_name = fi.file_name or f"file_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}.bin"
        ctx.media_path = self._download_service.save_media_to_file(data, media_dir, file_name)
        ctx.media_type = _get_mime_type(file_name)

    def _process_video_item(self, ctx: MessageContext, item: MessageItem, media_dir: str):
        vi = item.video_item
        if not vi or not vi.media or not vi.media.encrypt_query_param or not vi.media.aes_key:
            return
        data = self._download_service.download_video(vi.media.encrypt_query_param, vi.media.aes_key)
        ts = datetime.utcnow().strftime('%Y%m%d_%H%M%S')
        ctx.media_path = self._download_service.save_media_to_file(data, media_dir, f"video_{ts}.mp4")
        ctx.media_type = "video/mp4"


def extract_body_from_item_list(item_list: Optional[List[MessageItem]]) -> str:
    if not item_list:
        return ""
    for item in item_list:
        if item.type == MessageItemType.TEXT and item.text_item and item.text_item.text is not None:
            text = item.text_item.text
            ref_msg = item.ref_message
            if not ref_msg:
                return text
            if ref_msg.message_item and is_media_item(ref_msg.message_item):
                return text
            parts = []
            if ref_msg.title:
                parts.append(ref_msg.title)
            if ref_msg.message_item:
                ref_body = extract_body_from_item_list([ref_msg.message_item])
                if ref_body:
                    parts.append(ref_body)
            if not parts:
                return text
            return f"[引用: {' | '.join(parts)}]\n{text}"
        if item.type == MessageItemType.VOICE and item.voice_item and item.voice_item.text is not None:
            return item.voice_item.text
    return ""


def is_media_item(item: MessageItem) -> bool:
    return item.type in (MessageItemType.IMAGE, MessageItemType.VIDEO,
                         MessageItemType.FILE, MessageItemType.VOICE)


_MIME_MAP = {
    '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.gif': 'image/gif',
    '.pdf': 'application/pdf', '.doc': 'application/msword',
    '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    '.xls': 'application/vnd.ms-excel',
    '.xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    '.zip': 'application/zip', '.mp3': 'audio/mpeg', '.mp4': 'video/mp4',
}

def _get_mime_type(file_name: str) -> str:
    _, ext = os.path.splitext(file_name.lower())
    return _MIME_MAP.get(ext, 'application/octet-stream')
