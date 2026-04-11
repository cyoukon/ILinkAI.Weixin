package com.ilinkai.weixin.sample.helpers;

/**
 * 控制台二维码显示辅助类
 * 由于不引入第三方QR库，仅显示URL链接
 */
public class ConsoleQRCodeHelper {

    public static void displayQRCodeWithBorder(String url, String title) {
        System.out.println();
        System.out.println("═══════════════════════════════════");
        System.out.println("  " + title);
        System.out.println("═══════════════════════════════════");
        System.out.println();
        System.out.println("请在浏览器中打开以下链接扫码:");
        System.out.println(url);
        System.out.println();
    }
}
