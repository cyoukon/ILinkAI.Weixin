"""消息发送服务"""
import base64
import logging
import os
import uuid
from typing import Optional

from ..models import (
    SendMessageRequest, WeixinMessage, MessageItem, MessageItemType,
    MessageType, MessageState, TypingStatus, SendTypingRequest,
    CdnMedia, TextItem, ImageItem, VideoItem, FileItem,
)
from ..media.media_upload_service import MediaUploadService

logger = logging.getLogger(__name__)


class MessageSendService:
    def __init__(self, api_client, cdn_base_url: str):
        self._api_client = api_client
        self._upload_service = MediaUploadService(api_client, cdn_base_url)

    @staticmethod
    def _generate_client_id() -> str:
        return f"ilinkai-weixin-{uuid.uuid4().hex}"

    def send_text(self, to_user_id: str, text: str, context_token: str) -> str:
        if not context_token:
            raise ValueError("contextToken is required")
        client_id = self._generate_client_id()
        items = []
        if text:
            ti = TextItem(text=text)
            items.append(MessageItem(type=MessageItemType.TEXT, text_item=ti))

        msg = WeixinMessage(
            from_user_id="", to_user_id=to_user_id, client_id=client_id,
            message_type=MessageType.BOT, message_state=MessageState.FINISH,
            item_list=items, context_token=context_token,
        )
        request = SendMessageRequest(message=msg)
        self._api_client.send_message(request)
        return client_id

    def send_image(self, to_user_id: str, file_path: str, context_token: str,
                   caption: Optional[str] = None) -> str:
        if not context_token:
            raise ValueError("contextToken is required")
        uploaded = self._upload_service.upload_image(file_path, to_user_id)
        media = CdnMedia(
            encrypt_query_param=uploaded.download_encrypted_query_param,
            aes_key=base64.b64encode(bytes.fromhex(uploaded.aes_key)).decode(),
            encrypt_type=1,
        )
        image_item = MessageItem(
            type=MessageItemType.IMAGE,
            image_item=ImageItem(media=media, mid_size=uploaded.file_size_ciphertext),
        )
        return self._send_media_items(to_user_id, caption, image_item, context_token, "SendImage")

    def send_video(self, to_user_id: str, file_path: str, context_token: str,
                   caption: Optional[str] = None) -> str:
        if not context_token:
            raise ValueError("contextToken is required")
        uploaded = self._upload_service.upload_video(file_path, to_user_id)
        media = CdnMedia(
            encrypt_query_param=uploaded.download_encrypted_query_param,
            aes_key=base64.b64encode(bytes.fromhex(uploaded.aes_key)).decode(),
            encrypt_type=1,
        )
        video_item = MessageItem(
            type=MessageItemType.VIDEO,
            video_item=VideoItem(media=media, video_size=uploaded.file_size_ciphertext),
        )
        return self._send_media_items(to_user_id, caption, video_item, context_token, "SendVideo")

    def send_file(self, to_user_id: str, file_path: str, context_token: str,
                  caption: Optional[str] = None) -> str:
        if not context_token:
            raise ValueError("contextToken is required")
        file_name = os.path.basename(file_path)
        uploaded = self._upload_service.upload_file_attachment(file_path, to_user_id)
        media = CdnMedia(
            encrypt_query_param=uploaded.download_encrypted_query_param,
            aes_key=base64.b64encode(bytes.fromhex(uploaded.aes_key)).decode(),
            encrypt_type=1,
        )
        file_item = MessageItem(
            type=MessageItemType.FILE,
            file_item=FileItem(media=media, file_name=file_name, length=str(uploaded.file_size)),
        )
        return self._send_media_items(to_user_id, caption, file_item, context_token, "SendFile")

    def _send_media_items(self, to_user_id: str, text: Optional[str],
                           media_item: MessageItem, context_token: str, label: str) -> str:
        items = []
        if text:
            ti = TextItem(text=text)
            items.append(MessageItem(type=MessageItemType.TEXT, text_item=ti))
        items.append(media_item)

        last_client_id = ""
        for item in items:
            last_client_id = self._generate_client_id()
            msg = WeixinMessage(
                from_user_id="", to_user_id=to_user_id, client_id=last_client_id,
                message_type=MessageType.BOT, message_state=MessageState.FINISH,
                item_list=[item], context_token=context_token,
            )
            request = SendMessageRequest(message=msg)
            self._api_client.send_message(request)
        return last_client_id

    def send_typing(self, ilink_user_id: str, typing_ticket: str, is_typing: bool = True):
        request = SendTypingRequest(
            ilink_user_id=ilink_user_id, typing_ticket=typing_ticket,
            status=TypingStatus.TYPING if is_typing else TypingStatus.CANCEL,
        )
        self._api_client.send_typing(request)
