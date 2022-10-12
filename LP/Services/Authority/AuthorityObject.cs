// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityObject
{
    public AuthorityObject(string? seedPhrase, AuthorityLifetime lifetime, long lifeMics)
    {
        if (seedPhrase == null)
        {
            this.seed = new byte[Hash.HashBytes];
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

    internal AuthorityObject()
    {
    }

    public byte[]? SignData(Credit credit, byte[] data)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        var signature = privateKey.SignData(data);
        this.privateKeyCache.Cache(credit, privateKey);
        return signature;
    }

    public bool VerifyData(Credit credit, byte[] data, byte[] signature)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        var result = privateKey.VerifyData(data, signature);
        this.privateKeyCache.Cache(credit, privateKey);
        return result;
    }

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifetime Lifetime { get; init; }

    [Key(2)]
    public long LifeMics { get; init; }

    [Key(3)]
    public Value[] Values { get; init; } = Array.Empty<Value>();

    private PrivateKey GetOrCreatePrivateKey(Credit credit)
    {
        var privateKey = this.privateKeyCache.TryGet(credit);
        if (privateKey == null)
        {// Create private key.
            var hash = Hash.ObjectPool.Get();
            hash.HashUpdate(this.seed);
            hash.HashUpdate(TinyhandSerializer.Serialize(credit));
            var seed = hash.HashFinal();
            Hash.ObjectPool.Return(hash);

            privateKey = PrivateKey.Create(seed);
        }

        return privateKey;
    }

    private void CachePrivateKey(Credit credit, PrivateKey privateKey)
        => this.privateKeyCache.Cache(credit, privateKey);

    private ObjectCache<Credit, PrivateKey> privateKeyCache = new(10);

    public override string ToString()
        => $"Lifetime: {this.Lifetime}, LifeMics: {this.LifeMics}";
}
