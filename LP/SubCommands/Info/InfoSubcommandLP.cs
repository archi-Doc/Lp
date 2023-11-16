// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

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
                ByteArrayPool.Default.Dump(logger);
            }
        }
        else
        {
            var saa = DateTime.MinValue.Ticks;
            logger?.Log(Environment.OSVersion.ToString());
            logger?.Log($"Time.GetApplication(): {Time.GetApplication()}");
            logger?.Log($"Time.GetCorrected(): {Time.GetCorrected()}");

            logger?.Log($"Terminal:");
            this.Control.NetControl.Terminal.Dump(logger);

            logger?.Log($"Alternative:");
            if (this.Control.NetControl.Alternative is { } terminal)
            {
                terminal.Dump(logger);
            }
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
