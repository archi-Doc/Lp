// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("lp")]
public class InfoSubcommandLP : ISimpleCommand<DumpSubcommandInfoOptions>
{
    public InfoSubcommandLP(Control control)
    {
        this.Control = control;
    }

    public void Run(DumpSubcommandInfoOptions options, string[] args)
    {
        var target = args.Length > 0 ? args[0] : string.Empty;
        var logger = this.Control.Logger.TryGet<InfoSubcommandLP>(LogLevel.Information);

        logger?.Log($"Info: {target}");

        if (string.Compare("bytearraypool", target, true) == 0)
        {
            if (logger != null)
            {
                BlockPool.Dump(logger);
            }
        }
        else
        {
            logger?.Log(Environment.OSVersion.ToString());
            this.Control.NetControl.Terminal.Dump(logger);
        }
    }

    public Control Control { get; set; }
}

public record DumpSubcommandInfoOptions
{
    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Count}";
}
