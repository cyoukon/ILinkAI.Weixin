using System.Security.Cryptography;

namespace ILinkai.Weixin.Sdk.Cdn;

/// <summary>
/// AES-128-ECB加密工具类
/// 用于CDN上传和下载的加密解密操作
/// </summary>
public static class AesEcbCrypto
{
    /// <summary>
    /// 使用AES-128-ECB加密数据（PKCS7填充）
    /// </summary>
    /// <param name="plaintext">明文数据</param>
    /// <param name="key">16字节AES密钥</param>
    /// <returns>密文数据</returns>
    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        if (key.Length != 16)
        {
            throw new ArgumentException("AES key must be 16 bytes", nameof(key));
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    /// <summary>
    /// 使用AES-128-ECB解密数据（PKCS7填充）
    /// </summary>
    /// <param name="ciphertext">密文数据</param>
    /// <param name="key">16字节AES密钥</param>
    /// <returns>明文数据</returns>
    public static byte[] Decrypt(byte[] ciphertext, byte[] key)
    {
        if (key.Length != 16)
        {
            throw new ArgumentException("AES key must be 16 bytes", nameof(key));
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <summary>
    /// 计算AES-128-ECB加密后的密文大小（PKCS7填充到16字节边界）
    /// </summary>
    /// <param name="plaintextSize">明文大小</param>
    /// <returns>密文大小</returns>
    public static int GetPaddedSize(int plaintextSize)
    {
        return (int)Math.Ceiling((plaintextSize + 1) / 16.0) * 16;
    }

    /// <summary>
    /// 生成随机AES密钥
    /// </summary>
    /// <returns>16字节随机密钥</returns>
    public static byte[] GenerateKey()
    {
        var key = new byte[16];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
