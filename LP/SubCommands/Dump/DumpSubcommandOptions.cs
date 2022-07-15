// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("options")]
public class DumpSubcommandOptions : ISimpleCommandAsync<DumpSubcommandOptionsOptions>
{
    public DumpSubcommandOptions(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(DumpSubcommandOptionsOptions options, string[] args)
    {
        /*var output = options.Output;
        if (string.IsNullOrEmpty(output))
        {
            output = LPOptions.DefaultOptionsName;
        }

        var path = Path.Combine(this.Control.LPBase.RootDirectory, output);*/

        try
        {
            var utf = TinyhandSerializer.SerializeToUtf8(this.Control.LPBase.Options with { OptionsPath = string.Empty, });

            var path = this.Control.LPBase.CombineDataPathAndPrepareDirectory(options.Output, LPOptions.DefaultOptionsName);
            await File.WriteAllBytesAsync(path, utf);
            Logger.Default.Information(Hashed.Success.Output, path);
        }
        catch
        {
        }
    }

    public Control Control { get; set; }
}

public record DumpSubcommandOptionsOptions
{
    [SimpleOption("output", description: "Output name")]
    public string Output { get; init; } = string.Empty;

    public override string ToString() => $"{this.Output}";
}
