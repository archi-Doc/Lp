// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Netsphere.Crypto;

namespace Lp.Services;

#pragma warning disable SA1204 // Static elements should appear before instance elements

[TinyhandObject]
public partial class MasterKey : IStringConvertible<MasterKey>
{
    public const int Size = 64;

    public enum Kind : byte
    {
        Merger,
        RelayMerger,
        Linker,
    }

    #region IStringConvertible

    static MasterKey()
    {
        MaxStringLength = Base64.Url.GetEncodedLength(Size);
    }

    public static int MaxStringLength { get; }

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out MasterKey masterKey, out int read)
    {
        if (source.Length < MaxStringLength)
        {
            masterKey = null;
            read = 0;
            return false;
        }

        var seed = new byte[Size];
        if (!Base64.Url.FromStringToSpan(source.Slice(0, MaxStringLength), seed, out _))
        {
            masterKey = null;
            read = 0;
            return false;
        }

        masterKey = new MasterKey(seed);
        read = MaxStringLength;
        return true;
    }

    public int GetStringLength()
        => MaxStringLength;

    public bool TryFormat(Span<char> destination, out int written)
        => Base64.Url.FromByteArrayToSpan(this.seed, destination, out written);

    #endregion

    public static MasterKey New()
    {
        var seed = new byte[Size];
        RandomVault.Default.NextBytes(seed);
        return new MasterKey(seed);
    }

    [Key(0)]
    private byte[] seed;

    private MasterKey(byte[] seed)
    {
        if (seed.Length != Size)
        {
            throw new ArgumentException($"Seed must be {Size} bytes long.", nameof(seed));
        }

        this.seed = seed;
    }

    public (string Seedphrase, SeedKey SeedKey) CreateSeedKey(Kind kind)
         => this.GetKey(kind);

    private (string Seedphrase, SeedKey SeedKey) GetKey(Kind kind)
    {
        var size = Seedphrase.DefaultNumberOfWords * sizeof(ushort);

        Span<byte> cipher = stackalloc byte[size];
        var span = cipher;
        this.seed.AsSpan().CopyTo(span);
        span = span.Slice(this.seed.Length);
        span.Fill((byte)kind);

        Span<byte> key32 = stackalloc byte[Aegis256.KeySize];
        Blake3.Get256_Span(this.seed, key32);

        Span<byte> nonce32 = stackalloc byte[Aegis256.NonceSize];
        this.seed.AsSpan(0, Aegis256.NonceSize).CopyTo(nonce32);
        nonce32[0] ^= (byte)kind;
        nonce32[16] ^= (byte)kind;

        Aegis256.Encrypt(cipher, cipher, nonce32, key32, default, 0);
        var array = MemoryMarshal.Cast<byte, ushort>(cipher);
        var seedphrase = Seedphrase.Create();
        var seed = Seedphrase.TryGetSeed(seedphrase);
        return (seedphrase, SeedKey.New(seed, KeyOrientation.Signature));
    }
}
