"""数据模型定义 - 对应C#版本的Models目录"""
from dataclasses import dataclass, field
from typing import Optional, List


# ===== 枚举常量 =====
class UploadMediaType:
    IMAGE = 1
    VIDEO = 2
    FILE = 3
    VOICE = 4

class MessageType:
    NONE = 0
    USER = 1
    BOT = 2

class MessageItemType:
    NONE = 0
    TEXT = 1
    IMAGE = 2
    VOICE = 3
    FILE = 4
    VIDEO = 5

class MessageState:
    NEW = 0
    GENERATING = 1
    FINISH = 2

class TypingStatus:
    TYPING = 1
    CANCEL = 2

class VoiceEncodeType:
    PCM = 1; ADPCM = 2; FEATURE = 3; SPEEX = 4
    AMR = 5; SILK = 6; MP3 = 7; OGG_SPEEX = 8

class QRCodeStatus:
    WAIT = "wait"
    SCANNED = "scaned"
    CONFIRMED = "confirmed"
    EXPIRED = "expired"


# ===== 消息模型 =====

def to_dict(obj):
    """将对象转换为字典，忽略None值"""
    if obj is None:
        return None
    if isinstance(obj, dict):
        return {k: to_dict(v) for k, v in obj.items() if v is not None}
    if isinstance(obj, list):
        return [to_dict(i) for i in obj]
    if hasattr(obj, '__dataclass_fields__'):
        result = {}
        for f_name, f_def in obj.__dataclass_fields__.items():
            val = getattr(obj, f_name)
            if val is not None:
                key = f_def.metadata.get('json', f_name) if f_def.metadata else f_name
                result[key] = to_dict(val)
        return result
    return obj


def _field(json_name: str, default=None):
    """创建带JSON映射的dataclass字段"""
    return field(default=default, metadata={'json': json_name})


@dataclass
class BaseInfo:
    channel_version: Optional[str] = _field('channel_version')


@dataclass
class CdnMedia:
    encrypt_query_param: Optional[str] = _field('encrypt_query_param')
    aes_key: Optional[str] = _field('aes_key')
    encrypt_type: Optional[int] = _field('encrypt_type')


@dataclass
class TextItem:
    text: Optional[str] = _field('text')


@dataclass
class ImageItem:
    media: Optional[CdnMedia] = _field('media')
    thumb_media: Optional[CdnMedia] = _field('thumb_media')
    aes_key: Optional[str] = _field('aeskey')
    url: Optional[str] = _field('url')
    mid_size: Optional[int] = _field('mid_size')
    thumb_size: Optional[int] = _field('thumb_size')
    thumb_height: Optional[int] = _field('thumb_height')
    thumb_width: Optional[int] = _field('thumb_width')
    hd_size: Optional[int] = _field('hd_size')


@dataclass
class VoiceItem:
    media: Optional[CdnMedia] = _field('media')
    encode_type: Optional[int] = _field('encode_type')
    bits_per_sample: Optional[int] = _field('bits_per_sample')
    sample_rate: Optional[int] = _field('sample_rate')
    play_time: Optional[int] = _field('playtime')
    text: Optional[str] = _field('text')


@dataclass
class FileItem:
    media: Optional[CdnMedia] = _field('media')
    file_name: Optional[str] = _field('file_name')
    md5: Optional[str] = _field('md5')
    length: Optional[str] = _field('len')


@dataclass
class VideoItem:
    media: Optional[CdnMedia] = _field('media')
    video_size: Optional[int] = _field('video_size')
    play_length: Optional[int] = _field('play_length')
    video_md5: Optional[str] = _field('video_md5')
    thumb_media: Optional[CdnMedia] = _field('thumb_media')
    thumb_size: Optional[int] = _field('thumb_size')
    thumb_height: Optional[int] = _field('thumb_height')
    thumb_width: Optional[int] = _field('thumb_width')


@dataclass
class RefMessage:
    message_item: Optional['MessageItem'] = _field('message_item')
    title: Optional[str] = _field('title')


@dataclass
class MessageItem:
    type: Optional[int] = _field('type')
    create_time_ms: Optional[int] = _field('create_time_ms')
    update_time_ms: Optional[int] = _field('update_time_ms')
    is_completed: Optional[bool] = _field('is_completed')
    msg_id: Optional[str] = _field('msg_id')
    ref_message: Optional[RefMessage] = _field('ref_msg')
    text_item: Optional[TextItem] = _field('text_item')
    image_item: Optional[ImageItem] = _field('image_item')
    voice_item: Optional[VoiceItem] = _field('voice_item')
    file_item: Optional[FileItem] = _field('file_item')
    video_item: Optional[VideoItem] = _field('video_item')


@dataclass
class WeixinMessage:
    seq: Optional[int] = _field('seq')
    message_id: Optional[int] = _field('message_id')
    from_user_id: Optional[str] = _field('from_user_id')
    to_user_id: Optional[str] = _field('to_user_id')
    client_id: Optional[str] = _field('client_id')
    create_time_ms: Optional[int] = _field('create_time_ms')
    update_time_ms: Optional[int] = _field('update_time_ms')
    delete_time_ms: Optional[int] = _field('delete_time_ms')
    session_id: Optional[str] = _field('session_id')
    group_id: Optional[str] = _field('group_id')
    message_type: Optional[int] = _field('message_type')
    message_state: Optional[int] = _field('message_state')
    item_list: Optional[List[MessageItem]] = _field('item_list')
    context_token: Optional[str] = _field('context_token')


# ===== API请求/响应模型 =====

@dataclass
class GetUploadUrlRequest:
    file_key: Optional[str] = _field('filekey')
    media_type: Optional[int] = _field('media_type')
    to_user_id: Optional[str] = _field('to_user_id')
    raw_size: Optional[int] = _field('rawsize')
    raw_file_md5: Optional[str] = _field('rawfilemd5')
    file_size: Optional[int] = _field('filesize')
    thumb_raw_size: Optional[int] = _field('thumb_rawsize')
    thumb_raw_file_md5: Optional[str] = _field('thumb_rawfilemd5')
    thumb_file_size: Optional[int] = _field('thumb_filesize')
    no_need_thumb: Optional[bool] = _field('no_need_thumb')
    aes_key: Optional[str] = _field('aeskey')
    base_info: Optional[BaseInfo] = _field('base_info')


@dataclass
class GetUploadUrlResponse:
    upload_param: Optional[str] = None
    thumb_upload_param: Optional[str] = None


@dataclass
class GetUpdatesRequest:
    get_updates_buf: Optional[str] = _field('get_updates_buf')
    base_info: Optional[BaseInfo] = _field('base_info')


@dataclass
class GetUpdatesResponse:
    ret: Optional[int] = None
    errcode: Optional[int] = None
    errmsg: Optional[str] = None
    messages: Optional[List[WeixinMessage]] = None
    get_updates_buf: Optional[str] = None
    longpolling_timeout_ms: Optional[int] = None


@dataclass
class SendMessageRequest:
    message: Optional[WeixinMessage] = _field('msg')


@dataclass
class SendTypingRequest:
    ilink_user_id: Optional[str] = _field('ilink_user_id')
    typing_ticket: Optional[str] = _field('typing_ticket')
    status: Optional[int] = _field('status')
    base_info: Optional[BaseInfo] = _field('base_info')


@dataclass
class GetConfigRequest:
    ilink_user_id: Optional[str] = _field('ilink_user_id')
    context_token: Optional[str] = _field('context_token')
    base_info: Optional[BaseInfo] = _field('base_info')


@dataclass
class GetConfigResponse:
    ret: Optional[int] = None
    errmsg: Optional[str] = None
    typing_ticket: Optional[str] = None


@dataclass
class GetQRCodeResponse:
    qrcode: Optional[str] = None
    qrcode_img_content: Optional[str] = None


@dataclass
class GetQRCodeStatusResponse:
    status: Optional[str] = None
    bot_token: Optional[str] = None
    ilink_bot_id: Optional[str] = None
    baseurl: Optional[str] = None
    ilink_user_id: Optional[str] = None


# ===== JSON解析辅助 =====

def _parse_cdn_media(d: dict) -> Optional[CdnMedia]:
    if not d: return None
    return CdnMedia(
        encrypt_query_param=d.get('encrypt_query_param'),
        aes_key=d.get('aes_key'),
        encrypt_type=d.get('encrypt_type'),
    )

def _parse_message_item(d: dict) -> MessageItem:
    item = MessageItem(type=d.get('type'), create_time_ms=d.get('create_time_ms'),
                       update_time_ms=d.get('update_time_ms'), is_completed=d.get('is_completed'),
                       msg_id=d.get('msg_id'))
    if d.get('text_item'):
        item.text_item = TextItem(text=d['text_item'].get('text'))
    if d.get('image_item'):
        ii = d['image_item']
        item.image_item = ImageItem(
            media=_parse_cdn_media(ii.get('media')), thumb_media=_parse_cdn_media(ii.get('thumb_media')),
            aes_key=ii.get('aeskey'), url=ii.get('url'), mid_size=ii.get('mid_size'),
            thumb_size=ii.get('thumb_size'), thumb_height=ii.get('thumb_height'),
            thumb_width=ii.get('thumb_width'), hd_size=ii.get('hd_size'))
    if d.get('voice_item'):
        vi = d['voice_item']
        item.voice_item = VoiceItem(
            media=_parse_cdn_media(vi.get('media')), encode_type=vi.get('encode_type'),
            bits_per_sample=vi.get('bits_per_sample'), sample_rate=vi.get('sample_rate'),
            play_time=vi.get('playtime'), text=vi.get('text'))
    if d.get('file_item'):
        fi = d['file_item']
        item.file_item = FileItem(
            media=_parse_cdn_media(fi.get('media')), file_name=fi.get('file_name'),
            md5=fi.get('md5'), length=fi.get('len'))
    if d.get('video_item'):
        vd = d['video_item']
        item.video_item = VideoItem(
            media=_parse_cdn_media(vd.get('media')), video_size=vd.get('video_size'),
            play_length=vd.get('play_length'), video_md5=vd.get('video_md5'),
            thumb_media=_parse_cdn_media(vd.get('thumb_media')), thumb_size=vd.get('thumb_size'),
            thumb_height=vd.get('thumb_height'), thumb_width=vd.get('thumb_width'))
    if d.get('ref_msg'):
        rm = d['ref_msg']
        ref = RefMessage(title=rm.get('title'))
        if rm.get('message_item'):
            ref.message_item = _parse_message_item(rm['message_item'])
        item.ref_message = ref
    return item

def parse_weixin_message(d: dict) -> WeixinMessage:
    msg = WeixinMessage(
        seq=d.get('seq'), message_id=d.get('message_id'),
        from_user_id=d.get('from_user_id'), to_user_id=d.get('to_user_id'),
        client_id=d.get('client_id'), create_time_ms=d.get('create_time_ms'),
        update_time_ms=d.get('update_time_ms'), delete_time_ms=d.get('delete_time_ms'),
        session_id=d.get('session_id'), group_id=d.get('group_id'),
        message_type=d.get('message_type'), message_state=d.get('message_state'),
        context_token=d.get('context_token'))
    if d.get('item_list'):
        msg.item_list = [_parse_message_item(i) for i in d['item_list']]
    return msg
