// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using static LP.AuthorityInterface;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityInfo
{
    public static readonly uint SeedLength = Hash.HashBytes;

    public AuthorityInfo(string? seedPhrase, AuthorityLifetime lifetime, long lifeMics)
    {
        if (seedPhrase == null)
        {
            this.seed = new byte[SeedLength];
            Random.Crypto.NextBytes(this.seed);
        }
        else
        {
            var hash = Hash.ObjectPool.Get();
            var utf8 = System.Text.Encoding.UTF8.GetBytes(seedPhrase);
            this.seed = hash.GetHash(hash.GetHash(utf8));
            Hash.ObjectPool.Return(hash);
        }

        this.Lifetime = lifetime;
        this.LifeMics = lifeMics;
    }

    internal AuthorityInfo()
    {
    }

    public void SignData(Credit credit, byte[] data)
    {
        // Create private key.
    }

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifetime Lifetime { get; init; }

    [Key(2)]
    public long LifeMics { get; init; }

    [Key(3)]
    public Value[] Values { get; init; } = Array.Empty<Value>();

    public override string ToString()
        => $"Lifetime: {this.Lifetime}, LifeMics: {this.LifeMics}";
}
