// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class TestSubCommand : ISimpleCommandAsync<TestOptions>
{
    public async Task Run(TestOptions options, string[] args)
    {
        Logger.Subcommand.Information($"Test subcommand");
    }
}

public record TestOptions
{
    [SimpleOption("node", description: "Node address")]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
