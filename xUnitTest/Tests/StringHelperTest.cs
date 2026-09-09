// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Lp;
using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class StringHelperTest
{
    private const string Control = "\u0001";

    [Fact]
    public void AppendPrefixReturnsNothingForAnEmptyMessage()
    {
        var (rent, length) = StringHelper.AppendPrefix("> ", string.Empty);
        Assert.Equal(0, length);
        Assert.Empty(rent);
    }

    [Theory]
    [InlineData("abc", "> abc")]
    [InlineData("a\nb", "> a\n> b")]
    [InlineData("a\r\nb", "> a\n> b")]
    [InlineData("a\n", "> a\n> ")]
    public void AppendPrefixPrefixesEveryLineAndDropsCarriageReturns(string message, string expected)
    {
        var (rent, length) = StringHelper.AppendPrefix("> ", message);
        try
        {
            Assert.Equal(expected, new string(rent, 0, length));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rent);
        }
    }

    [Fact]
    public void MergerStringUsesTheSeparatorSymbolBetweenMergers()
    {
        var merger1 = SeedKey.NewSignature().GetSignaturePublicKey();
        var merger2 = SeedKey.NewSignature().GetSignaturePublicKey();

        Assert.Equal($"/{merger1}", new[] { merger1 }.ToMergerString(default));
        Assert.Equal($"/{merger1}+{merger2}", new[] { merger1, merger2 }.ToMergerString(default));
        Assert.Equal(string.Empty, Array.Empty<SignaturePublicKey>().ToMergerString(default));
    }

    [Fact]
    public void MergerStringRefusesMoreMergersThanTheStackBufferHolds()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var tooMany = Enumerable.Repeat(key, LpConstants.MaxMergers + 1).ToArray();
        Assert.Equal(string.Empty, tooMany.ToMergerString(default));
    }

    [Theory]
    [InlineData("'quoted'", "quoted")]
    [InlineData("''", "")]
    [InlineData("'", "'")]
    [InlineData("unquoted", "unquoted")]
    [InlineData("'mismatched", "'mismatched")]
    public void UnwrapQuoteRemovesOnlyAMatchingPairOfSingleQuotes(string input, string expected)
        => Assert.Equal(expected, input.UnwrapQuote());

    [Theory]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("  padded  ", "padded")]
    [InlineData("   ", "")]
    [InlineData("ab", "ab")]
    [InlineData(" a", "a")]
    [InlineData("a ", "a")]
    [InlineData("a\tb", "ab")]
    [InlineData("a\u0001b", "ab")]
    [InlineData(" a\u0001b ", "ab")]
    [InlineData("\u0001ab", "ab")]
    [InlineData("ab\u0001", "ab")]
    [InlineData("\u0001", "")]
    public void CleanupInputTrimsWhitespaceAndDropsControlCharacters(string input, string expected)
        => Assert.Equal(expected, input.CleanupInput());

    [Fact]
    public void CleanupInputReturnsTheSameInstanceWhenNothingChanges()
    {
        var input = "nothing to clean";
        Assert.Same(input, input.CleanupInput());

        var large = new string('x', 1024);
        Assert.Same(large, large.CleanupInput());
    }

    [Fact]
    public void CleanupInputHandlesInputsLargerThanTheStackBuffer()
    {
        // The implementation switches strategy above 256 characters.
        var withControl = "  " + new string('x', 600) + Control + new string('y', 600) + "  ";
        Assert.Equal(new string('x', 600) + new string('y', 600), withControl.CleanupInput());

        var trimmedOnly = "  " + new string('x', 600) + "  ";
        Assert.Equal(new string('x', 600), trimmedOnly.CleanupInput());
    }

    [Fact]
    public void ShortAndLongInputsAreCleanedByTheSameRules()
    {
        var padding = new string('a', 300);
        Assert.Equal("bc", (" b" + Control + "c ").CleanupInput());
        Assert.Equal(padding + "bc", (" " + padding + "b" + Control + "c ").CleanupInput());
    }

    [Fact]
    public void SerializeRoundTripsThroughTheStrictStringFormat()
    {
        var assignment = new DomainAssignment("name", "code", CertificateProof.UnsafeConstructor());
        var text = StringHelper.SerializeToString(assignment);
        var restored = StringHelper.DeserializeFromString<DomainAssignment>(text);
        Assert.NotNull(restored);
        Assert.Equal("name", restored.Name);
        Assert.Equal("code", restored.Code);
    }

    [Fact]
    public void MalformedTextDeserializesToNullInsteadOfThrowing()
        => Assert.Null(StringHelper.DeserializeFromString<DomainAssignment>("{ this is not tinyhand"));
}
