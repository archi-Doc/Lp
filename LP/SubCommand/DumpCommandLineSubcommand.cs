// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("dumpcommandline")]
public class DumpCommandLineSubcommand : ISimpleCommandAsync<DumpCommandLineOptions>
{
    public const string DefaultName = "commandline";

    public DumpCommandLineSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(DumpCommandLineOptions options, string[] args)
    {
        var output = options.Output;
        if (string.IsNullOrEmpty(output))
        {
            output = DefaultName;
        }

        var path = Path.Combine(this.Control.LPBase.RootDirectory, output);
        Logger.Subcommand.Information("Dump command line.");
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

public record DumpCommandLineOptions
{
    [SimpleOption("name", description: "Output name")]
    public string Output { get; init; } = string.Empty;

    public override string ToString() => $"{this.Output}";
}
