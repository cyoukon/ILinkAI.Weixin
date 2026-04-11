"""完整工作流示例"""
import os
import tempfile
from datetime import datetime

from samples.example_base import ExampleBase
from samples.login_example import LoginExample
from samples.llm_service import LlmService, LlmConfig
from ilinkai_weixin import WeixinApiClient, WeixinApiClientOptions
from ilinkai_weixin.auth.account_store import AccountStore
from ilinkai_weixin.messaging.message_send_service import MessageSendService
from ilinkai_weixin.messaging.message_monitor_service import MessageMonitorService
from ilinkai_weixin.models import MessageItemType


class FullWorkflowExample(ExampleBase):
    name = "full"
    description = "完整工作流示例"
    usage = "full"

    def __init__(self):
        self._login_example = LoginExample()

    def execute(self, args: list) -> int:
        self.print_header("完整工作流示例")
        print("此示例演示完整的登录->监控->回复流程")
        print()

        account_store = AccountStore()
        accounts = account_store.list_account_ids()

        if not accounts:
            print("没有已登录账户，开始登录流程...")
            print()
            login_result = self._login_example.execute(args)
            if login_result != 0:
                return login_result
            accounts = account_store.list_account_ids()
            account_id = accounts[-1]
        else:
            print("请选择账户:")
            print("  [0] 新登录账户")
            for i, acc in enumerate(accounts):
                print(f"  [{i + 1}] {acc}")
            print()

            selection = -1
            while selection < 0 or selection > len(accounts):
                try:
                    selection = int(input(f"请输入选择 (0-{len(accounts)}): ").strip())
                except (ValueError, EOFError):
                    selection = -1

            if selection == 0:
                print()
                login_result = self._login_example.execute(args)
                if login_result != 0:
                    return login_result
                accounts = account_store.list_account_ids()
                account_id = accounts[-1]
            else:
                account_id = accounts[selection - 1]

        account = account_store.resolve_account(account_id)
        print(f"使用账户: {account_id}")
        print()

        # 选择回复模式
        use_llm = False
        llm_service = None

        print("选择回复模式:")
        print("  [1] Echo 模式 - 回显收到的消息详情")
        print("  [2] LLM 模式 - 调用大模型对话接口回复")
        mode_input = input("请选择 (1/2, 默认 1): ").strip()

        if mode_input == "2":
            config = LlmConfig()
            base_url = input(f"API Base URL (默认 {config.base_url}): ").strip()
            if base_url:
                config.base_url = base_url

            api_key = input("API Key: ").strip()
            if not api_key:
                print("API Key 不能为空，回退到 Echo 模式。")
            else:
                config.api_key = api_key
                model = input(f"模型名称 (默认 {config.model}): ").strip()
                if model:
                    config.model = model
                prompt = input("系统提示词 (可选，回车跳过): ").strip()
                if prompt:
                    config.system_prompt = prompt
                llm_service = LlmService(config)
                use_llm = True

        client = WeixinApiClient(WeixinApiClientOptions(
            base_url=account.base_url, token=account.token))
        send_service = MessageSendService(client, account.cdn_base_url)

        media_dir = os.path.join(tempfile.gettempdir(), "ilinkai-weixin", "media")
        monitor = MessageMonitorService(
            base_url=account.base_url, cdn_base_url=account.cdn_base_url,
            token=account.token, account_id=account.account_id, media_dir=media_dir)

        def on_message(ctx):
            print()
            print("========================================")
            print(f"收到消息: {ctx.body}")
            print(f"发送者: {ctx.from_}")

            if not ctx.context_token:
                print("⚠ 无 ContextToken，跳过回复")
            else:
                try:
                    if use_llm and llm_service:
                        if not ctx.body or not ctx.body.strip():
                            reply = "(收到非文本消息，暂不支持 LLM 处理)"
                        else:
                            print("   🤖 正在调用大模型...")
                            reply = llm_service.chat(ctx.body)
                    else:
                        reply = _format_echo_reply(ctx.original_message)
                    send_service.send_text(ctx.from_, reply, ctx.context_token)
                    print("已自动回复：" + reply)
                except Exception as ex:
                    print(f"回复失败: {ex}")
            print("========================================")

        def on_error(err):
            print(f"错误: {err}")

        monitor.on_message_received = on_message
        monitor.on_error = on_error

        print("开始监控并自动回复消息，按 Ctrl+C 停止...")
        print()

        try:
            monitor.start()
        except KeyboardInterrupt:
            print("\n已停止")
        return 0


def _format_echo_reply(msg):
    if not msg:
        return "(空消息)"
    lines = ["📋 消息详情:"]
    lines.append(f"  消息ID: {msg.message_id}")
    lines.append(f"  发送者: {msg.from_user_id}")
    lines.append(f"  接收者: {msg.to_user_id}")
    if msg.create_time_ms:
        time_str = datetime.fromtimestamp(msg.create_time_ms / 1000).strftime("%Y-%m-%d %H:%M:%S")
        lines.append(f"  时间: {time_str}")
    if msg.item_list:
        for item in msg.item_list:
            t = item.type or 0
            type_names = {
                MessageItemType.TEXT: "文本", MessageItemType.IMAGE: "图片",
                MessageItemType.VOICE: "语音", MessageItemType.FILE: "文件",
                MessageItemType.VIDEO: "视频",
            }
            type_name = type_names.get(t, f"未知({t})")
            preview = _get_item_preview(item)
            lines.append(f"  [{type_name}] {preview}")
    return "\n".join(lines)


def _get_item_preview(item):
    t = item.type or 0
    if t == MessageItemType.TEXT:
        return item.text_item.text if item.text_item else ""
    if t == MessageItemType.IMAGE and item.image_item:
        return f"尺寸: {item.image_item.thumb_width}x{item.image_item.thumb_height}"
    if t == MessageItemType.VOICE and item.voice_item:
        return f"时长: {item.voice_item.play_time}ms | 转文字: {item.voice_item.text or '无'}"
    if t == MessageItemType.FILE and item.file_item:
        return f"{item.file_item.file_name} ({item.file_item.length} bytes)"
    if t == MessageItemType.VIDEO and item.video_item:
        return f"时长: {item.video_item.play_length}s"
    return ""
