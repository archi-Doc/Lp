// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;

namespace LP.T3CS;

[TinyhandObject]
public sealed partial class Authority
{
    private const int MinimumSeedLength = 32;

    public Authority(byte[]? seed, AuthorityLifetime lifetime, long lifeMics)
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

    public override int GetHashCode()
        => this.hash != 0 ? this.hash : (this.hash = (int)FarmHash.Hash64(this.seed));

    public void SignProof<T>(T proof, long proofMics)
        where T : Proof, ITinyhandSerialize<T>
    {
        var privateKey = this.GetOrCreatePrivateKey();
        proof.SignProof<T>(privateKey, proofMics);
    }

    public void SignToken<T>(T token)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        var privateKey = this.GetOrCreatePrivateKey();
        Netsphere.TinyhandHelper.Sign(token, privateKey);
    }

    /*public void SignToken(Credit credit, Token token)
    {
        var privateKey = this.GetOrCreatePrivateKey(credit);
        token.Sign(privateKey);
    }*/

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

    public SignaturePublicKey PublicKey => this.GetOrCreatePrivateKey().ToPublicKey();

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifetime Lifetime { get; private set; }

    [Key(2)]
    public long LifeMics { get; private set; }

    [Key(3)]
    // public Value[] Values { get; private set; } = Array.Empty<Value>();
    public Value Values { get; private set; } = default!;

    private int hash;

    private SignaturePrivateKey GetOrCreatePrivateKey()
        => this.GetOrCreatePrivateKey(Credit.Default);

    private SignaturePrivateKey GetOrCreatePrivateKey(Credit credit)
    {
        var privateKey = this.privateKeyCache.TryGet(credit);
        if (privateKey == null)
        {// Create private key.
            var buffer = TinyhandHelper.RentBuffer();
            var writer = new Tinyhand.IO.TinyhandWriter(buffer);
            try
            {
                writer.WriteSpan(this.seed);
                TinyhandSerializer.SerializeObject(ref writer, credit);

                Span<byte> span = stackalloc byte[32];
                writer.FlushAndGetReadOnlySpan(out var input, out _);
                Sha3Helper.Get256_Span(input, span);
                privateKey = SignaturePrivateKey.Create(span);
            }
            finally
            {
                writer.Dispose();
                TinyhandHelper.ReturnBuffer(buffer);
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
