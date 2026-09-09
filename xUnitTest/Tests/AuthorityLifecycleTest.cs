// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class AuthorityLifecycleTest
{
    [Fact]
    public void AShortSeedDoesNotBreakHashing()
    {
        // The parameterless constructor is used by deserialization and leaves the seed empty.
        var authority = new Authority();
        var exception = Record.Exception(() => authority.GetHashCode());
        Assert.Null(exception);
    }

    [Fact]
    public void AMissingOrShortSeedIsReplacedByARandomOne()
    {
        var withoutSeed = new Authority(null, AuthorityLifecycle.Application, 0);
        var withShortSeed = new Authority([1, 2, 3], AuthorityLifecycle.Application, 0);
        Assert.NotEqual(withoutSeed.GetSignaturePublicKey(), withShortSeed.GetSignaturePublicKey());
        Assert.NotEqual(0, withoutSeed.GetHashCode());
    }

    [Fact]
    public void ADurationAuthorityStartsExpiredUntilItsExpirationIsReset()
    {
        var authority = new Authority(new byte[32], AuthorityLifecycle.Duration, Mics.FromMinutes(10));
        Assert.True(authority.IsExpired);

        authority.ResetExpirationMics();
        Assert.False(authority.IsExpired);
    }

    [Fact]
    public void AnApplicationAuthorityNeverExpiresAndIgnoresResets()
    {
        var authority = new Authority(new byte[32], AuthorityLifecycle.Application, Mics.FromMinutes(10));
        Assert.False(authority.IsExpired);
        Assert.Equal(0, authority.ExpirationMics);

        authority.ResetExpirationMics();
        Assert.Equal(0, authority.ExpirationMics);
        Assert.False(authority.IsExpired);
    }

    [Fact]
    public void KeysDerivedFromTheSameSeedAndCreditAreIdentical()
    {
        var seed = new byte[32];
        seed[0] = 7;
        var merger = SeedKey.NewSignature().GetSignaturePublicKey();
        var credit = new Credit(default, [merger]);

        var first = new Authority(seed, AuthorityLifecycle.Application, 0);
        var second = new Authority(seed, AuthorityLifecycle.Application, 0);

        Assert.Equal(first.GetSignaturePublicKey(), second.GetSignaturePublicKey());
        Assert.Equal(first.GetSignaturePublicKey(credit), second.GetSignaturePublicKey(credit));
        Assert.Equal(first.GetEncryptionPublicKey(), second.GetEncryptionPublicKey());
        Assert.Equal(first.GetEncryptionPublicKey(credit), second.GetEncryptionPublicKey(credit));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentCreditsProduceDifferentKeys()
    {
        var authority = new Authority(new byte[32], AuthorityLifecycle.Application, 0);
        var credit1 = new Credit(default, [SeedKey.NewSignature().GetSignaturePublicKey()]);
        var credit2 = new Credit(default, [SeedKey.NewSignature().GetSignaturePublicKey()]);
        Assert.NotEqual(authority.GetSignaturePublicKey(credit1), authority.GetSignaturePublicKey(credit2));
    }

    [Fact]
    public void SerializationPreservesTheDerivedKeys()
    {
        var authority = new Authority(new byte[32], AuthorityLifecycle.Duration, Mics.FromMinutes(1));
        var restored = TinyhandSerializer.Deserialize<Authority>(TinyhandSerializer.Serialize(authority));
        Assert.NotNull(restored);
        Assert.Equal(authority.GetSignaturePublicKey(), restored.GetSignaturePublicKey());
        Assert.Equal(authority.Lifecycle, restored.Lifecycle);
        Assert.Equal(authority.DurationMics, restored.DurationMics);
    }

    [Fact]
    public void DescriptionMentionsTheLifecycle()
        => Assert.Contains("Application", new Authority(new byte[32], AuthorityLifecycle.Application, 0).ToString());
}
