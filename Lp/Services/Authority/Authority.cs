// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Services;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Authority
{
    public const string Name = "Authority";
    private const int MinimumSeedLength = 32;

    public static Authority? GetFromVault(Vault vault)
    {
        if (vault.TryGetObject<Authority>(Name, out var authority, out _))
        {
            authority.Vault = vault;
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
    public Vault? Vault { get; internal set; }

    [IgnoreMember]
    public long ExpirationMics { get; private set; }

    [IgnoreMember]
    private ConcurrentDictionary<Credit, SeedKey> seedKeyCache = new();

    #endregion

    public Authority(byte[]? seed, AuthorityLifecycle lifecycle, long durationMics)
    {
        if (seed == null || seed.Length < MinimumSeedLength)
        {
            this.seed = new byte[MinimumSeedLength];
            RandomVault.Default.NextBytes(this.seed);
        }
        else
        {
            this.seed = seed;
        }

        this.Lifecycle = lifecycle;
        this.DurationMics = durationMics;
    }

    internal Authority()
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
        => this.seedKeyCache.GetOrAdd(credit, CreateSeedKey(this.seed, credit));

    public EncryptionPublicKey GetEncryptionPublicKey()
        => this.GetSeedKey(Credit.Default).GetEncryptionPublicKey();

    public EncryptionPublicKey GetEncryptionPublicKey(Credit credit)
        => this.GetSeedKey(credit).GetEncryptionPublicKey();

    public SignaturePublicKey GetSignaturePublicKey()
        => this.GetSeedKey(Credit.Default).GetSignaturePublicKey();

    public SignaturePublicKey GetSignaturePublicKey(Credit credit)
        => this.GetSeedKey(credit).GetSignaturePublicKey();

    public override int GetHashCode()
        => BitConverter.ToInt32(this.seed.AsSpan());

    public override string ToString()
        => $"PublicKey: {this.GetSeedKey().ToString()}, Lifetime: {this.Lifecycle}, DurationMics: {this.DurationMics}";
}
