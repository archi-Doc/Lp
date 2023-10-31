// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Xunit;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace xUnitTest;

public class StringExtentionTest
{
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
