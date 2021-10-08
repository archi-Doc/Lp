// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace LP;

public static class Random
{
    public const int VaultSize = 1024;

    static Random()
    {
        var xo = new Xoshiro256StarStar();
        Pseudo = new RandomVault(() => xo.NextULong(), x => xo.NextBytes(x), VaultSize);
        Crypto = new RandomVault(null, x => RandomNumberGenerator.Fill(x), VaultSize);
    }

    public static unsafe byte[] GenerateGenePool(ReadOnlySpan<byte> source)
    { // length <= 3k ?, 3k x 8 bytes = 24 kbytes
        var hash = XXHash64.Hash64(source);
        var xo = new Xoshiro256StarStar(hash);

        Span<byte> span = stackalloc byte[source.Length * sizeof(ulong)];
        xo.NextBytes(span);

        var aes = Aes.Create();
        aes.KeySize = 128;

        // Span<byte> key = stackalloc byte[16];
        var key = new byte[16];
        xo.NextBytes(key);
        aes.Key = key;
        // Span<byte> iv = stackalloc byte[16];
        var iv = new byte[16];
        xo.NextBytes(iv);
        aes.IV = iv;

        // MemoryMarshal.AsBytes(span);
        var result = aes.EncryptCbc(span, iv);
        return result;

        // var encryptor = aes.CreateEncryptor(key, iv);
    }

    public static RandomVault Crypto { get; }

    public static RandomVault Pseudo { get; }
}
