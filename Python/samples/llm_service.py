"""大模型对话服务，支持 OpenAI 兼容接口"""
import json
from dataclasses import dataclass, field
from typing import Optional
from urllib.request import Request, urlopen
from urllib.error import HTTPError


@dataclass
class LlmConfig:
    base_url: str = "https://api.siliconflow.cn"
    api_key: str = ""
    model: str = "deepseek-ai/DeepSeek-R1-0528-Qwen3-8B"
    system_prompt: Optional[str] = None
    max_tokens: int = 1024
    temperature: float = 0.7


class LlmService:
    def __init__(self, config: LlmConfig):
        self._config = config

    def chat(self, user_message: str) -> str:
        url = self._config.base_url.rstrip('/') + '/v1/chat/completions'
        system_prompt = self._config.system_prompt or "你是一个处理提单的小助手。"
        body = json.dumps({
            "model": self._config.model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_message},
            ],
            "max_tokens": self._config.max_tokens,
            "temperature": self._config.temperature,
        }).encode('utf-8')

        req = Request(url, data=body, method='POST')
        req.add_header('Content-Type', 'application/json')
        req.add_header('Authorization', f'Bearer {self._config.api_key}')

        try:
            with urlopen(req, timeout=60) as resp:
                data = json.loads(resp.read().decode('utf-8'))
                content = data.get('choices', [{}])[0].get('message', {}).get('content')
                return content or "(空回复)"
        except HTTPError as e:
            body_text = e.read().decode('utf-8', errors='replace') if e.fp else ''
            return f"[LLM 调用失败: HTTP {e.code}] {body_text[:200]}"
        except Exception as e:
            return f"[LLM 调用异常: {e}]"
