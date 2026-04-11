"""示例注册表"""
from collections import OrderedDict

from samples.login_example import LoginExample
from samples.send_example import SendExample
from samples.monitor_example import MonitorExample
from samples.account_example import AccountExample
from samples.upload_example import UploadExample
from samples.download_example import DownloadExample
from samples.config_example import ConfigExample
from samples.typing_example import TypingExample
from samples.full_workflow_example import FullWorkflowExample


class ExampleRegistry:
    def __init__(self):
        self._examples = OrderedDict()
        self._register_defaults()

    def _register_defaults(self):
        for ex in [LoginExample(), SendExample(), MonitorExample(),
                   AccountExample(), UploadExample(), DownloadExample(),
                   ConfigExample(), TypingExample(), FullWorkflowExample()]:
            self._examples[ex.name.lower()] = ex

    def get_example(self, name: str):
        return self._examples.get(name.lower())

    def print_usage(self):
        print("用法: python main.py <命令> [参数]")
        print()
        print("命令:")
        for ex in self._examples.values():
            print(f"  {ex.name:<18}- {ex.description}")
        print()
        print("示例:")
        print('  python main.py login')
        print('  python main.py send --to user@im.wechat --text "Hello" --token xxx --context-token xxx')
        print('  python main.py monitor --account-id xxx --token xxx')
        print('  python main.py account list')
