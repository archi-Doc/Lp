// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILoggerSource<TestSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(TestOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Test subcommand: {options.ToString()}");
        // this.Control.Netsphere.NetStatus

        // this.logger.TryGet()?.Log(System.Environment.OSVersion.ToString());
    }

    public Control Control { get; set; }

    private ILoggerSource<TestSubcommand> logger;
}

public record TestOptions
{
    [SimpleOption("node", description: "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", description: "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
