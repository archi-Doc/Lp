// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(TestOptions options, string[] args)
    {
        Logger.Subcommand.Information($"Test subcommand: {options.ToString()}");
        // this.Control.Netsphere.NetStatus

        // Logger.Subcommand.Information(System.Environment.OSVersion.ToString());
    }

    public Control Control { get; set; }
}

public record TestOptions
{
    [SimpleOption("node", description: "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", description: "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
