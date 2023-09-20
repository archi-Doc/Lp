// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace LP.T3CS;

[TinyhandObject]
public sealed partial class Authority
{
    public Authority(string? seedPhrase, AuthorityLifetime lifetime, long lifeMics)
    {
        if (seedPhrase == null)
        {
            this.seed = new byte[Hash.HashBytes]; // 32 bytes
            RandomVault.Crypto.NextBytes(this.seed);
        }
        else
        {
            var utf8 = System.Text.Encoding.UTF8.GetBytes(seedPhrase);
            this.seed = Sha3Helper.Get256_ByteArray(Sha3Helper.Get256_ByteArray(utf8));
        }

        this.Lifetime = lifetime;
        this.LifeMics = lifeMics;
    }

    internal Authority()
    {
    }

    public override int GetHashCode()
        => this.hash != 0 ? this.hash : (this.hash = (int)FarmHash.Hash64(this.seed));

    public void SignToken(Token token)
    {
        var privateKey = this.GetOrCreatePrivateKey();
        token.Sign(privateKey);
    }

    public void SignToken(Credit credit, Token token)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        token.Sign(privateKey);
    }

    public byte[]? SignData(Credit credit, byte[] data)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        var signature = privateKey.SignData(data);
        this.CachePrivateKey(credit, privateKey);
        return signature;
    }

    public bool VerifyData(Credit credit, byte[] data, byte[] signature)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        var result = privateKey.VerifyData(data, signature);
        this.CachePrivateKey(credit, privateKey);
        return result;
    }

    public PublicKey PublicKey => this.GetOrCreatePrivateKey().ToPublicKey();

    [Key(0)]
    private byte[] seed = [];

    [Key(1)]
    public AuthorityLifetime Lifetime { get; private set; }

    [Key(2)]
    public long LifeMics { get; private set; }

    [Key(3)]
    // public Value[] Values { get; private set; } = Array.Empty<Value>();
    public Value Values { get; private set; } = default!;

    private int hash;

    private PrivateKey GetOrCreatePrivateKey()
        => this.GetOrCreatePrivateKey(Credit.Default);

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

            privateKey = PrivateKey.CreateVerificationKey(seed);
        }

        return privateKey;
    }

    private void CachePrivateKey(Credit credit, PrivateKey privateKey)
        => this.privateKeyCache.Cache(credit, privateKey);

    private ObjectCache<Credit, PrivateKey> privateKeyCache = new(10);

    public override string ToString()
        => $"PublicKey: {this.GetOrCreatePrivateKey().ToPublicKey()}, Lifetime: {this.Lifetime}, LifeMics: {this.LifeMics}";
}
