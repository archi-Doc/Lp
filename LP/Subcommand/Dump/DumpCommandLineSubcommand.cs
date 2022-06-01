// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("options")]
public class DumpSubcommandOptions : ISimpleCommandAsync<DumpSubcommandOptions2>
{
    public const string DefaultName = "commandline";

    public DumpSubcommandOptions(Control control)
    {
        this.Control = control;
    }

    public async Task Run(DumpSubcommandOptions2 options, string[] args)
    {
        var output = options.Output;
        if (string.IsNullOrEmpty(output))
        {
            output = DefaultName;
        }

        var path = Path.Combine(this.Control.LPBase.RootDirectory, output);
        Logger.Subcommand.Information($"Output: {path}");

        try
        {
            var st = TinyhandSerializer.SerializeToString(this.Control.LPBase.ConsoleOptions);
            await File.WriteAllTextAsync(path, st);
        }
        catch
        {
        }
    }

    public Control Control { get; set; }
}

public record DumpSubcommandOptions2
{
    [SimpleOption("output", description: "Output name")]
    public string Output { get; init; } = string.Empty;

    public override string ToString() => $"{this.Output}";
}
