// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Authority2
{
    public const string Name = "Authority";
    private const int MinimumSeedLength = 32;

    public static Authority2? FromVault(Vault vault)
    {
        if (vault.TryGetObject<Authority2>(Name, out var authority, out _))
        {
            return authority;
        }
        else
        {
            vault.ParentVault.Remove(name);//
            return default;
        }
    }

    public Authority2(byte[]? seed, AuthorityLifecycle lifecycle, long lifeMics)
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

        this.Lifecycle = lifecycle;
        this.LifeMics = lifeMics;
    }

    internal Authority2()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifecycle Lifecycle { get; private set; }

    [IgnoreMember]
    public long ExpirationMics { get; private set; }

    #endregion

    public void ResetExpirationMics()
        => this.ExpirationMics = Mics.FastUtcNow;

    public bool IsExpired()
    {
        if (this.Lifecycle == AuthorityLifecycle.Duration)
        {
            return this.ExpirationMics < Mics.FastUtcNow;
        }
        else
        {
            return false;
        }
    }

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

    public string UnsafeToString()
        => this.GetOrCreatePrivateKey().UnsafeToString() ?? string.Empty;

    public override string ToString()
        => $"PublicKey: {this.GetOrCreatePrivateKey().ToPublicKey()}, Lifetime: {this.Lifecycle}, LifeMics: {this.LifeMics}";
}
