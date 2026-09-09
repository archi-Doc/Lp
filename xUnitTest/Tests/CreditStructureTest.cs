// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Lp;
using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CreditStructureTest
{
    [Fact]
    public void CreationRejectsTooFewOrTooManyMergers()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        Assert.False(Credit.TryCreate(default, [], out _));
        Assert.False(Credit.TryCreate(default, Enumerable.Repeat(key, LpConstants.MaxMergers + 1).ToArray(), out _));
        Assert.True(Credit.TryCreate(default, [key], out var credit));
        Assert.Equal(1, credit.MergerCount);
        Assert.Equal(key, credit.PrimaryMerger);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Credit(default, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Credit(default, Enumerable.Repeat(key, LpConstants.MaxMergers + 1).ToArray()));
    }

    [Fact]
    public void TheDefaultCreditHasNoMergerAndDoesNotValidate()
    {
        Assert.Equal(0, Credit.Default.MergerCount);
        Assert.False(Credit.Default.Validate());
        Assert.Equal(default, Credit.Default.PrimaryMerger);
    }

    [Fact]
    public void GetMergerIndexReportsThePositionOfEachMerger()
    {
        var keys = Enumerable.Range(0, LpConstants.MaxMergers)
            .Select(_ => SeedKey.NewSignature().GetSignaturePublicKey())
            .ToArray();
        var absent = SeedKey.NewSignature().GetSignaturePublicKey();

        for (var count = 1; count <= LpConstants.MaxMergers; count++)
        {
            var credit = new Credit(default, keys.Take(count).ToArray());
            Assert.True(credit.Validate());
            for (var i = 0; i < count; i++)
            {
                var key = keys[i];
                Assert.Equal(i, credit.GetMergerIndex(ref key));
                Assert.Equal(key, credit.GetMerger((byte)i));
            }

            for (var i = count; i < LpConstants.MaxMergers; i++)
            {
                var key = keys[i];
                Assert.Equal(-1, credit.GetMergerIndex(ref key));
            }

            Assert.Equal(-1, credit.GetMergerIndex(ref absent));

            // An out-of-range index falls back to the primary merger.
            Assert.Equal(credit.PrimaryMerger, credit.GetMerger(byte.MaxValue));
        }
    }

    [Fact]
    public void GetMergerIndexReturnsMinusOneForACreditWithoutMergers()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        Assert.Equal(-1, Credit.Default.GetMergerIndex(ref key));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void StringConversionRoundTripsForEveryMergerCount(int mergerCount)
    {
        var keys = Enumerable.Range(0, mergerCount)
            .Select(_ => SeedKey.NewSignature().GetSignaturePublicKey())
            .ToArray();
        var credit = new Credit(default, keys);

        var text = credit.ToString();
        Assert.True(Credit.TryParse(text, out var parsed, out var read));
        Assert.Equal(text.Length, read);
        Assert.True(credit.Equals(parsed));
        Assert.Equal(credit.GetHashCode(), parsed.GetHashCode());

        var destination = new char[credit.GetStringLength()];
        Assert.True(credit.TryFormat(destination, out var written));
        Assert.Equal(text.Length, written);
        Assert.Equal(text, new string(destination, 0, written));
    }

    [Fact]
    public void FormattingFailsInsteadOfOverrunningAShortBuffer()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var credit = new Credit(default, [key]);
        var length = credit.GetStringLength();

        Assert.False(credit.TryFormat(Span<char>.Empty, out var written));
        Assert.Equal(0, written);

        var text = credit.ToString();
        Assert.True(length >= text.Length);
        Assert.False(credit.TryFormat(new char[text.Length - 1], out written));
    }

    [Theory]
    [InlineData("")]
    [InlineData("@")]
    [InlineData("Originator/Merger")]
    [InlineData("@Originator")]
    public void MalformedTextIsRejected(string text)
        => Assert.False(Credit.TryParse(text, out _, out _));

    [Fact]
    public void EqualityDistinguishesIdentifiersAndMergerLists()
    {
        var key1 = SeedKey.NewSignature().GetSignaturePublicKey();
        var key2 = SeedKey.NewSignature().GetSignaturePublicKey();

        var credit = new Credit(default, [key1]);
        Assert.False(credit.Equals(null));
        Assert.True(credit.Equals(new Credit(default, [key1])));
        Assert.False(credit.Equals(new Credit(default, [key2])));
        Assert.False(credit.Equals(new Credit(default, [key1, key2])));
    }

    [Fact]
    public void CreditIdentityValidatesItsMergerList()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        Assert.False(new CreditIdentity(default, key, []).Validate());
        Assert.False(new CreditIdentity(default, default, [key]).Validate());
        Assert.False(new CreditIdentity(default, key, [default]).Validate());

        var identity = new CreditIdentity(default, key, [key]);
        Assert.True(identity.Validate());
        Assert.NotNull(identity.ToCredit());
        Assert.Null(new CreditIdentity(default, key, []).ToCredit());
    }

    [Fact]
    public void CreditIdentityDescriptionIsBalanced()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var text = new CreditIdentity(default, key, [key]).ToString();
        Assert.Equal(text.Count(x => x == '{'), text.Count(x => x == '}'));
    }
}
