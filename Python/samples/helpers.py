"""控制台二维码显示辅助"""


def display_qrcode_with_border(url: str, title: str = "请扫描二维码"):
    print()
    print("═══════════════════════════════════")
    print(f"  {title}")
    print("═══════════════════════════════════")
    print()
    print("请在浏览器中打开以下链接扫码:")
    print(url)
    print()
