// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Authority
{
    private const int MinimumSeedLength = 32;

    public Authority(byte[]? seed, AuthorityLifecycle lifetime, long lifeMics)
    {
        if (seed == null || seed.Length < MinimumSeedLength)
        {
            this.seed = new byte[MinimumSeedLength];
            RandomVault.Crypto.NextBytes(this.seed);
        }
        else
        {
            this.seed = seed;
        }

        this.Lifetime = lifetime;
        this.LifeMics = lifeMics;
    }

    internal Authority()
    {
    }

    #region FieldAndProperty

    public SignaturePublicKey PublicKey => this.GetOrCreatePrivateKey().ToPublicKey();

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifecycle Lifetime { get; private set; }

    [Key(2)]
    public long LifeMics { get; private set; }

    [Key(3)]
    // public Value[] Values { get; private set; } = Array.Empty<Value>();
    public Value Values { get; private set; } = default!;

    private int hash;

    #endregion

    public bool TrySignEvidence(Evidence evidence, int mergerIndex)
    {
        var privateKey = this.GetOrCreatePrivateKey();
        return evidence.TrySign(privateKey, mergerIndex);
    }

    public void SignProof(Proof proof, long validMics)
    {
        var privateKey = this.GetOrCreatePrivateKey();
        proof.SignProof(privateKey, validMics);
    }

    public void SignWithSalt<T>(T token, ulong salt)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        token.Salt = salt;
        var privateKey = this.GetOrCreatePrivateKey();
        NetHelper.Sign(token, privateKey);
    }

    private SignaturePrivateKey GetOrCreatePrivateKey()
    {// this.GetOrCreatePrivateKey(Credit.Default);
        var privateKey = this.privateKeyCache.TryGet(Credit.Default);
        if (privateKey == null)
        {// Create private key.
            privateKey = SignaturePrivateKey.Create(this.seed);
            this.CachePrivateKey(Credit.Default, privateKey);
        }

        return privateKey;
    }

    private void CachePrivateKey(Credit credit, SignaturePrivateKey privateKey)
        => this.privateKeyCache.Cache(credit, privateKey);

    private ObjectCache<Credit, SignaturePrivateKey> privateKeyCache = new(10);

    public string UnsafeToString()
        => this.GetOrCreatePrivateKey().UnsafeToString() ?? string.Empty;

    public override string ToString()
        => $"PublicKey: {this.GetOrCreatePrivateKey().ToPublicKey()}, Lifetime: {this.Lifetime}, LifeMics: {this.LifeMics}";
}
