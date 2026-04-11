using QRCoder;

namespace ILinkai.Weixin.Sample.Helpers;

public static class ConsoleQRCodeHelper
{
    public static void DisplayQRCode(string url, int moduleSize = 2)
    {
        if (string.IsNullOrEmpty(url))
        {
            Console.WriteLine("错误: URL为空，无法生成二维码");
            return;
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new AsciiQRCode(qrCodeData);
        
        var qrCodeAsAscii = qrCode.GetGraphic(moduleSize, drawQuietZones: true);
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(qrCodeAsAscii);
        Console.ResetColor();
    }

    public static void DisplayQRCodeWithBorder(string url, string title = "请扫描二维码")
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"═══════════════════════════════════");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"═══════════════════════════════════");
        Console.ResetColor();
        
        DisplayQRCode(url);
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("或访问链接: " + url);
        Console.ResetColor();
        Console.WriteLine();
    }
}
