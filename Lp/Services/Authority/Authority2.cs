// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Services;
using Netsphere.Crypto;
using Netsphere.Crypto2;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Authority2
{
    public const string Name = "Authority";
    private const int MinimumSeedLength = 32;

    public static Authority2? GetFromVault(Vault vault)
    {
        if (vault.TryGetObject<Authority2>(Name, out var authority, out _))
        {
            return authority;
        }
        else
        {
            // vault.ParentVault?.Remove(name);
            return default;
        }
    }

    private static SeedKey CreateSeedKey(byte[] seed, Credit credit)
    {
        SeedKey seedKey;
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            writer.WriteSpan(seed);
            TinyhandSerializer.SerializeObject(ref writer, credit);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            Span<byte> s = stackalloc byte[Blake3.Size];
            Blake3.Get256_Span(span, s);
            seedKey = SeedKey.New(s, KeyOrientation.NotSpecified);
        }
        finally
        {
            writer.Dispose();
        }

        return seedKey;
    }

    #region FieldAndProperty

    [Key(0)]
    private byte[] seed = Array.Empty<byte>();

    [Key(1)]
    public AuthorityLifecycle Lifecycle { get; private set; }

    [Key(2)]
    public long DurationMics { get; private set; }

    [IgnoreMember]
    public long ExpirationMics { get; private set; }

    [IgnoreMember]
    private ConcurrentDictionary<Credit, SeedKey> creditToSeedKey = new();

    #endregion

    public Authority2(byte[]? seed, AuthorityLifecycle lifecycle, long durationMics)
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
        this.DurationMics = durationMics;
    }

    internal Authority2()
    {
    }

    public void ResetExpirationMics()
    {
        if (this.Lifecycle == AuthorityLifecycle.Duration)
        {
            this.ExpirationMics = Mics.FastUtcNow + this.DurationMics;
        }
    }

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

    public SeedKey GetSeedKey()
        => this.GetSeedKey(Credit.Default);

    public SeedKey GetSeedKey(Credit credit)
        => this.creditToSeedKey.GetOrAdd(credit, CreateSeedKey(this.seed, credit));

    public override int GetHashCode()
        => BitConverter.ToInt32(this.seed.AsSpan());

    /*public bool TrySignEvidence(Evidence evidence, int mergerIndex)
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
    }*/

    public override string ToString()
        => $"PublicKey: {this.GetSeedKey().ToString()}, Lifetime: {this.Lifecycle}, DurationMics: {this.DurationMics}";
}
