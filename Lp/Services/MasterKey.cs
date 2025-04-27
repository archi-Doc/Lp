// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    static bool IStringConvertible<MasterKey>.TryParse(ReadOnlySpan<char> source, out MasterKey? @object, out int read)
    {
        if (source.Length < MaxStringLength)
        {
            @object = null;
            read = 0;
            return false;
        }

        var seed = new byte[Size];
        if (!Base64.Url.FromStringToSpan(source.Slice(0, MaxStringLength), seed, out _))
        {
            @object = null;
            read = 0;
            return false;
        }

        @object = new MasterKey(seed);
        read = MaxStringLength;
        return true;
    }

    int IStringConvertible<MasterKey>.GetStringLength()
        => MaxStringLength;

    bool IStringConvertible<MasterKey>.TryFormat(Span<char> destination, out int written)
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

    private (string Seedphrase, SeedKey seedKey) GetKey(Kind kind)
    {
        Span<byte> hash = stackalloc byte[Blake3.Size];
        this.kindAndSeed[0] = (byte)kind;
        Blake3.Get256_Span(this.kindAndSeed, hash);


        using var hasher = Blake3Hasher.New();
        hasher.Update(additional);
        hasher.Update(previousSeed);
        hasher.Finalize(seed32);

        Span<byte> seed = stackalloc byte[Blake3.Size];
        if (!Seedphrase.TryAlter(this.seedphrase, [(byte)kind], seed))
        {
            return (string.Empty, SeedKey.NewSignature(seed));
        }
    }
}
