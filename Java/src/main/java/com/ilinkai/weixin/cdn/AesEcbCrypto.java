package com.ilinkai.weixin.cdn;

import javax.crypto.Cipher;
import javax.crypto.spec.SecretKeySpec;
import java.security.SecureRandom;

/** AES-128-ECB加密工具类 */
public class AesEcbCrypto {

    /** 使用AES-128-ECB加密数据（PKCS5Padding） */
    public static byte[] encrypt(byte[] plaintext, byte[] key) throws Exception {
        if (key.length != 16) throw new IllegalArgumentException("AES key must be 16 bytes");
        Cipher cipher = Cipher.getInstance("AES/ECB/PKCS5Padding");
        cipher.init(Cipher.ENCRYPT_MODE, new SecretKeySpec(key, "AES"));
        return cipher.doFinal(plaintext);
    }

    /** 使用AES-128-ECB解密数据（PKCS5Padding） */
    public static byte[] decrypt(byte[] ciphertext, byte[] key) throws Exception {
        if (key.length != 16) throw new IllegalArgumentException("AES key must be 16 bytes");
        Cipher cipher = Cipher.getInstance("AES/ECB/PKCS5Padding");
        cipher.init(Cipher.DECRYPT_MODE, new SecretKeySpec(key, "AES"));
        return cipher.doFinal(ciphertext);
    }

    /** 计算AES-128-ECB加密后的密文大小 */
    public static int getPaddedSize(int plaintextSize) {
        return (int) Math.ceil((plaintextSize + 1) / 16.0) * 16;
    }

    /** 生成随机AES密钥 */
    public static byte[] generateKey() {
        byte[] key = new byte[16];
        new SecureRandom().nextBytes(key);
        return key;
    }
}
