"""AES-128-ECB加密工具类 - 纯Python实现，无第三方依赖"""
import os
import math


# AES S-Box
_SBOX = [
    0x63,0x7c,0x77,0x7b,0xf2,0x6b,0x6f,0xc5,0x30,0x01,0x67,0x2b,0xfe,0xd7,0xab,0x76,
    0xca,0x82,0xc9,0x7d,0xfa,0x59,0x47,0xf0,0xad,0xd4,0xa2,0xaf,0x9c,0xa4,0x72,0xc0,
    0xb7,0xfd,0x93,0x26,0x36,0x3f,0xf7,0xcc,0x34,0xa5,0xe5,0xf1,0x71,0xd8,0x31,0x15,
    0x04,0xc7,0x23,0xc3,0x18,0x96,0x05,0x9a,0x07,0x12,0x80,0xe2,0xeb,0x27,0xb2,0x75,
    0x09,0x83,0x2c,0x1a,0x1b,0x6e,0x5a,0xa0,0x52,0x3b,0xd6,0xb3,0x29,0xe3,0x2f,0x84,
    0x53,0xd1,0x00,0xed,0x20,0xfc,0xb1,0x5b,0x6a,0xcb,0xbe,0x39,0x4a,0x4c,0x58,0xcf,
    0xd0,0xef,0xaa,0xfb,0x43,0x4d,0x33,0x85,0x45,0xf9,0x02,0x7f,0x50,0x3c,0x9f,0xa8,
    0x51,0xa3,0x40,0x8f,0x92,0x9d,0x38,0xf5,0xbc,0xb6,0xda,0x21,0x10,0xff,0xf3,0xd2,
    0xcd,0x0c,0x13,0xec,0x5f,0x97,0x44,0x17,0xc4,0xa7,0x7e,0x3d,0x64,0x5d,0x19,0x73,
    0x60,0x81,0x4f,0xdc,0x22,0x2a,0x90,0x88,0x46,0xee,0xb8,0x14,0xde,0x5e,0x0b,0xdb,
    0xe0,0x32,0x3a,0x0a,0x49,0x06,0x24,0x5c,0xc2,0xd3,0xac,0x62,0x91,0x95,0xe4,0x79,
    0xe7,0xc8,0x37,0x6d,0x8d,0xd5,0x4e,0xa9,0x6c,0x56,0xf4,0xea,0x65,0x7a,0xae,0x08,
    0xba,0x78,0x25,0x2e,0x1c,0xa6,0xb4,0xc6,0xe8,0xdd,0x74,0x1f,0x4b,0xbd,0x8b,0x8a,
    0x70,0x3e,0xb5,0x66,0x48,0x03,0xf6,0x0e,0x61,0x35,0x57,0xb9,0x86,0xc1,0x1d,0x9e,
    0xe1,0xf8,0x98,0x11,0x69,0xd9,0x8e,0x94,0x9b,0x1e,0x87,0xe9,0xce,0x55,0x28,0xdf,
    0x8c,0xa1,0x89,0x0d,0xbf,0xe6,0x42,0x68,0x41,0x99,0x2d,0x0f,0xb0,0x54,0xbb,0x16,
]

_INV_SBOX = [0] * 256
for _i, _v in enumerate(_SBOX):
    _INV_SBOX[_v] = _i

_RCON = [0x01,0x02,0x04,0x08,0x10,0x20,0x40,0x80,0x1b,0x36]


def _xtime(a):
    return ((a << 1) ^ 0x1b) & 0xff if a & 0x80 else (a << 1) & 0xff

def _mix_single(a, b):
    """Galois field multiplication"""
    p = 0
    for _ in range(8):
        if b & 1: p ^= a
        hi = a & 0x80
        a = (a << 1) & 0xff
        if hi: a ^= 0x1b
        b >>= 1
    return p

def _key_expansion(key: bytes) -> list:
    nk = 4; nr = 10; nb = 4
    w = [0] * (nb * (nr + 1))
    for i in range(nk):
        w[i] = int.from_bytes(key[4*i:4*i+4], 'big')
    for i in range(nk, nb * (nr + 1)):
        temp = w[i - 1]
        if i % nk == 0:
            # RotWord + SubWord + Rcon
            temp = ((temp << 8) | (temp >> 24)) & 0xffffffff
            temp = (_SBOX[(temp >> 24) & 0xff] << 24 | _SBOX[(temp >> 16) & 0xff] << 16 |
                    _SBOX[(temp >> 8) & 0xff] << 8 | _SBOX[temp & 0xff])
            temp ^= (_RCON[i // nk - 1] << 24)
        w[i] = w[i - nk] ^ temp
    return w

def _add_round_key(state, rk, rnd):
    for c in range(4):
        col = rk[rnd * 4 + c]
        for r in range(4):
            state[r][c] ^= (col >> (24 - 8 * r)) & 0xff

def _sub_bytes(state, sbox):
    for r in range(4):
        for c in range(4):
            state[r][c] = sbox[state[r][c]]

def _shift_rows(state):
    state[1][0], state[1][1], state[1][2], state[1][3] = state[1][1], state[1][2], state[1][3], state[1][0]
    state[2][0], state[2][1], state[2][2], state[2][3] = state[2][2], state[2][3], state[2][0], state[2][1]
    state[3][0], state[3][1], state[3][2], state[3][3] = state[3][3], state[3][0], state[3][1], state[3][2]

def _inv_shift_rows(state):
    state[1][0], state[1][1], state[1][2], state[1][3] = state[1][3], state[1][0], state[1][1], state[1][2]
    state[2][0], state[2][1], state[2][2], state[2][3] = state[2][2], state[2][3], state[2][0], state[2][1]
    state[3][0], state[3][1], state[3][2], state[3][3] = state[3][1], state[3][2], state[3][3], state[3][0]

def _mix_columns(state):
    for c in range(4):
        a = [state[r][c] for r in range(4)]
        state[0][c] = _xtime(a[0]) ^ _xtime(a[1]) ^ a[1] ^ a[2] ^ a[3]
        state[1][c] = a[0] ^ _xtime(a[1]) ^ _xtime(a[2]) ^ a[2] ^ a[3]
        state[2][c] = a[0] ^ a[1] ^ _xtime(a[2]) ^ _xtime(a[3]) ^ a[3]
        state[3][c] = _xtime(a[0]) ^ a[0] ^ a[1] ^ a[2] ^ _xtime(a[3])

def _inv_mix_columns(state):
    for c in range(4):
        a = [state[r][c] for r in range(4)]
        state[0][c] = _mix_single(a[0],14) ^ _mix_single(a[1],11) ^ _mix_single(a[2],13) ^ _mix_single(a[3],9)
        state[1][c] = _mix_single(a[0],9) ^ _mix_single(a[1],14) ^ _mix_single(a[2],11) ^ _mix_single(a[3],13)
        state[2][c] = _mix_single(a[0],13) ^ _mix_single(a[1],9) ^ _mix_single(a[2],14) ^ _mix_single(a[3],11)
        state[3][c] = _mix_single(a[0],11) ^ _mix_single(a[1],13) ^ _mix_single(a[2],9) ^ _mix_single(a[3],14)

def _encrypt_block(block: bytes, rk: list) -> bytes:
    state = [[0]*4 for _ in range(4)]
    for r in range(4):
        for c in range(4):
            state[r][c] = block[c * 4 + r]
    _add_round_key(state, rk, 0)
    for rnd in range(1, 10):
        _sub_bytes(state, _SBOX)
        _shift_rows(state)
        _mix_columns(state)
        _add_round_key(state, rk, rnd)
    _sub_bytes(state, _SBOX)
    _shift_rows(state)
    _add_round_key(state, rk, 10)
    out = bytearray(16)
    for r in range(4):
        for c in range(4):
            out[c * 4 + r] = state[r][c]
    return bytes(out)

def _decrypt_block(block: bytes, rk: list) -> bytes:
    state = [[0]*4 for _ in range(4)]
    for r in range(4):
        for c in range(4):
            state[r][c] = block[c * 4 + r]
    _add_round_key(state, rk, 10)
    for rnd in range(9, 0, -1):
        _inv_shift_rows(state)
        _sub_bytes(state, _INV_SBOX)
        _add_round_key(state, rk, rnd)
        _inv_mix_columns(state)
    _inv_shift_rows(state)
    _sub_bytes(state, _INV_SBOX)
    _add_round_key(state, rk, 0)
    out = bytearray(16)
    for r in range(4):
        for c in range(4):
            out[c * 4 + r] = state[r][c]
    return bytes(out)


class AesEcbCrypto:
    """AES-128-ECB加密工具类"""

    @staticmethod
    def encrypt(plaintext: bytes, key: bytes) -> bytes:
        """使用AES-128-ECB加密数据（PKCS7填充）"""
        if len(key) != 16:
            raise ValueError("AES key must be 16 bytes")
        rk = _key_expansion(key)
        # PKCS7 padding
        pad_len = 16 - (len(plaintext) % 16)
        padded = plaintext + bytes([pad_len] * pad_len)
        result = bytearray()
        for i in range(0, len(padded), 16):
            result.extend(_encrypt_block(padded[i:i+16], rk))
        return bytes(result)

    @staticmethod
    def decrypt(ciphertext: bytes, key: bytes) -> bytes:
        """使用AES-128-ECB解密数据（PKCS7填充）"""
        if len(key) != 16:
            raise ValueError("AES key must be 16 bytes")
        if len(ciphertext) % 16 != 0:
            raise ValueError("Ciphertext length must be multiple of 16")
        rk = _key_expansion(key)
        result = bytearray()
        for i in range(0, len(ciphertext), 16):
            result.extend(_decrypt_block(ciphertext[i:i+16], rk))
        # Remove PKCS7 padding
        pad_len = result[-1]
        if pad_len < 1 or pad_len > 16:
            raise ValueError("Invalid PKCS7 padding")
        return bytes(result[:-pad_len])

    @staticmethod
    def get_padded_size(plaintext_size: int) -> int:
        """计算AES-128-ECB加密后的密文大小"""
        return int(math.ceil((plaintext_size + 1) / 16.0)) * 16

    @staticmethod
    def generate_key() -> bytes:
        """生成随机AES密钥"""
        return os.urandom(16)
