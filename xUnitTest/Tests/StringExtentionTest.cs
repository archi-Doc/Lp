// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Xunit;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace xUnitTest;

public class StringExtentionTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData(" \t\r\n\0", "")]
    [InlineData("\0  abc  \0", "abc")]
    [InlineData("\u2003a\0b\u0085", "ab")]
    public void CleanupBoundaryCases(string input, string expected)
    {
        Assert.Equal(expected, input.CleanupInput());
    }

    [Fact]
    public void CleanupHandlesLargeInputsWithoutUnboundedStackAllocation()
    {
        var input = new string('a', 2_000_000);
        Assert.Same(input, input.CleanupInput());
        Assert.Equal(input, (" \0" + input + "\t ").CleanupInput());
        Assert.Equal(input, (input[..1_000_000] + "\0" + input[1_000_000..]).CleanupInput());
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("> ", "a\r\nb\n", "> a\n> b\n> ")]
    [InlineData("", "a\rb", "ab")]
    public void PrefixPreservesLineStructure(string prefix, string message, string expected)
    {
        var result = StringHelper.AppendPrefix(prefix, message);
        try
        {
            Assert.Equal(expected, new string(result.Rent, 0, result.Length));
        }
        finally
        {
            if (result.Rent.Length > 0)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(result.Rent);
            }
        }
    }

    [Fact]
    public void TestCleanupInput()
    {
        "".CleanupInput().Is("");
        "a".CleanupInput().Is("a");
        "abc".CleanupInput().Is("abc");

        " abc".CleanupInput().Is("abc");
        "abc ".CleanupInput().Is("abc");
        "a b c".CleanupInput().Is("a b c");
        " a b c ".CleanupInput().Is("a b c");

        "  abc\t".CleanupInput().Is("abc");
        " \t abc \t\t".CleanupInput().Is("abc");
        "a\r\nb\tc".CleanupInput().Is("abc");
        "a\r\n b \tc".CleanupInput().Is("a b c");
        " \ta\r\nb\tc\t  ".CleanupInput().Is("abc");
    }
}
