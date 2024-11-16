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

    public SignaturePrivateKey UnsafeGetPrivateKey()
        => this.GetOrCreatePrivateKey();

    public override int GetHashCode()
        => this.hash != 0 ? this.hash : (this.hash = (int)FarmHash.Hash64(this.seed));

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

    public void Sign<T>(T token)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        var privateKey = this.GetOrCreatePrivateKey();
        NetHelper.Sign(token, privateKey);
    }

    public void SignToken<T>(Credit credit, T token)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        NetHelper.Sign(token, privateKey);
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

    private SignaturePrivateKey GetOrCreatePrivateKey(Credit credit)
    {
        var privateKey = this.privateKeyCache.TryGet(credit);
        if (privateKey == null)
        {// Create private key.
            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                writer.WriteSpan(this.seed);
                TinyhandSerializer.SerializeObject(ref writer, credit);

                Span<byte> span = stackalloc byte[32];
                var rentMemory = writer.FlushAndGetRentMemory();
                Sha3Helper.Get256_Span(rentMemory.Span, span);
                rentMemory.Return();
                privateKey = SignaturePrivateKey.Create(span);
            }
            finally
            {
                writer.Dispose();
            }
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
