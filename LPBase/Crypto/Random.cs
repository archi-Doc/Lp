// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
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

    public static unsafe byte[] GenerateGenePool(ReadOnlySpan<byte> source, int destinationSize)
    { // length <= 3k ?, 3k x 8 bytes = 24 kbytes
        var hash = XXHash64.Hash64(source);
        var xo = new Xoshiro256StarStar(hash);

        byte[]? rentBytes = null;
        Span<byte> span = destinationSize <= 1024 ? stackalloc byte[destinationSize] : (rentBytes = ArrayPool<byte>.Shared.Rent(destinationSize));
        xo.NextBytes(span);

        var aes = Aes128.ObjectPool.Get();
        try
        {
            xo.NextBytes(aes.Key);
            aes.Aes.Key = aes.Key;
            xo.NextBytes(aes.IV);
            // aes.Aes.IV = aes.IV;
            var result = new byte[destinationSize];
            aes.Aes.TryEncryptCbc(span, aes.IV, result, out var written, PaddingMode.None);
            return result;
        }
        finally
        {
            Aes128.ObjectPool.Return(aes);
            if (rentBytes != null)
            {
                ArrayPool<byte>.Shared.Return(rentBytes);
            }
        }
    }

    public static RandomVault Crypto { get; }

    public static RandomVault Pseudo { get; }
}
