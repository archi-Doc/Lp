// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.Services;

public class ChainKey
{
    public enum Kind : byte
    {
        Merger,
        RelayMerger,
        Linker,
    }

    private readonly string seedphrase;
    private readonly byte[] kindAndSeed;

    public static bool TryCreate(string seedphrase, [MaybeNullWhen(false)] out ChainKey chainKey)
    {
        var seed = Seedphrase.TryGetSeed(seedphrase);
        if (seed is null)
        {
            chainKey = default;
            return false;
        }

        chainKey = new(seedphrase, seed);
        return true;
    }

    private ChainKey(string seedphrase, byte[] seed)
    {
        this.seedphrase = seedphrase;
        this.kindAndSeed = new byte[1 + seed.Length];
        this.kindAndSeed[0] = 0;
        seed.AsSpan().CopyTo(this.kindAndSeed.AsSpan(1));
    }

    public SeedKey GetMergerKey()
         => this.GetKey(Kind.Merger);

    private (string SeedPhrase, SeedKey seedKey) GetKey(Kind kind)
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
