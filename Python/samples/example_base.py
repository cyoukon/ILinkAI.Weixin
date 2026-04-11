"""示例基类"""
import sys


class ExampleBase:
    name: str = ""
    description: str = ""
    usage: str = ""

    def execute(self, args: list) -> int:
        raise NotImplementedError

    def print_header(self, title: str):
        print(f">>> {title}")
        print()

    def print_success(self, message: str):
        print(f"✅ {message}")

    def print_error(self, message: str):
        print(f"❌ {message}")

    def print_warning(self, message: str):
        print(f"⚠️ {message}")

    @staticmethod
    def parse_argument(args: list, name: str):
        for i in range(len(args) - 1):
            if args[i].lower() == name.lower():
                return args[i + 1]
        return None
