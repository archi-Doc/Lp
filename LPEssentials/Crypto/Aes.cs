// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Aes256
{
    public const int KeyBits = 256;
    public const int IVBits = 128;
    public const int KeyBytes = KeyBits / 8;
    public const int IVBytes = IVBits / 8;

    static Aes256()
    {
        NoPadding = Aes.Create();
        NoPadding.KeySize = KeyBits;
        NoPadding.Mode = CipherMode.CBC;
        NoPadding.Padding = PaddingMode.None;

        PKCS7 = Aes.Create();
        PKCS7.KeySize = KeyBits;
        PKCS7.Mode = CipherMode.CBC;
        PKCS7.Padding = PaddingMode.PKCS7;
    }

    public static Aes NoPadding { get; }

    public static Aes PKCS7 { get; }
}

public static class Aes128
{
    public const int KeyBits = 128;
    public const int IVBits = 128;
    public const int KeyBytes = KeyBits / 8;
    public const int IVBytes = IVBits / 8;

    static Aes128()
    {
        NoPadding = Aes.Create();
        NoPadding.KeySize = KeyBits;
        NoPadding.Mode = CipherMode.CBC;
        NoPadding.Padding = PaddingMode.None;

        PKCS7 = Aes.Create();
        PKCS7.KeySize = KeyBits;
        PKCS7.Mode = CipherMode.CBC;
        PKCS7.Padding = PaddingMode.PKCS7;
    }

    public static Aes NoPadding { get; }

    public static Aes PKCS7 { get; }
}
