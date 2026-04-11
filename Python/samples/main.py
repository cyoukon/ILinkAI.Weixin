#!/usr/bin/env python3
"""ILinkai Weixin SDK 使用示例"""
import sys
import os

# 确保能找到 SDK 和 samples 包
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from samples.example_registry import ExampleRegistry


def main():
    print("========================================")
    print("  ILinkai Weixin SDK 使用示例")
    print("========================================")
    print()

    args = sys.argv[1:]

    if not args:
        registry = ExampleRegistry()
        registry.print_usage()
        return 0

    command = args[0].lower()

    try:
        registry = ExampleRegistry()
        example = registry.get_example(command)

        if example:
            return example.execute(args)
        else:
            registry.print_usage()
            return 0
    except Exception as ex:
        print(f"执行命令时发生错误: {ex}", file=sys.stderr)
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
