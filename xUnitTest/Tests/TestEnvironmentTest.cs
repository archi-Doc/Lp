// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Lp;
using Lp.Subcommands;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class TestEnvironmentTest
{
    public TestEnvironmentTest(LpFixture fixture)
    {// The fixture loads the strings.
        _ = fixture;
    }

    [Fact]
    public async Task TheCachedClockFollowsTheCurrentTime()
    {// Proofs are validated against Mics.FastCorrected, which is updated by FastClockFixture during the tests.
        var start = Mics.FastCorrected;
        await Task.Delay(1_500, TestContext.Current.CancellationToken);
        Assert.True(Mics.FastCorrected - start >= Mics.FromSeconds(1));
    }

    [Fact]
    public void TheStringsAreLoadedAndFormatted()
    {
        Assert.Equal("Could not save 'a': b", HashedString.Get(Hashed.Error.Save, "a", "b"));
        Assert.Equal("The vault could not be loaded, so it was moved to 'a'", HashedString.Get(Hashed.Vault.Preserved, "a"));
    }

    [Fact]
    public void TheNodeHintNamesAnExistingCommand()
    {
        var command = typeof(AddNetNodeSubcommand).GetCustomAttribute<SimpleCommandAttribute>()!.CommandName;
        Assert.Contains($"({command})", HashedString.Get(Hashed.Error.NoOnlineNode));
    }
}
