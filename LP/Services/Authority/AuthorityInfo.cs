// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using static LP.AuthorityInterface;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityInfo
{
    public static readonly uint SeedLength = Hash.HashBytes;

    public AuthorityInfo(string? passPhrase, AuthorityLifetime lifetime, long lifeMics)
    {
        if (passPhrase == null)
        {
            this.Seed = new byte[SeedLength];
            Random.Crypto.NextBytes(this.Seed);
        }
        else
        {
            var hash = Hash.ObjectPool.Get();
            var utf8 = System.Text.Encoding.UTF8.GetBytes(passPhrase);
            this.Seed = hash.GetHash(hash.GetHash(utf8));
            Hash.ObjectPool.Return(hash);
        }

        this.Lifetime = lifetime;
        this.LifeMics = lifeMics;
    }

    internal AuthorityInfo()
    {
    }

    [Key(0)]
    public byte[] Seed { private get; init; } = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifetime Lifetime { get; init; }

    [Key(2)]
    public long LifeMics { get; init; }

    [Key(3)]
    public Value[] Values { get; init; } = Array.Empty<Value>();
}
