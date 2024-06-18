// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("options")]
public class ExportSubcommandOptions : ISimpleCommandAsync<ExportSubcommandOptionsOptions>
{
    public ExportSubcommandOptions(ILogger<ExportSubcommandOptions> logger, IUserInterfaceService userInterfaceService, Control control)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.Control = control;
    }

    public async Task RunAsync(ExportSubcommandOptionsOptions options, string[] args)
    {
        try
        {
            var utf = TinyhandSerializer.SerializeToUtf8(this.Control.LpBase.Options with { OptionsPath = string.Empty, });

            var path = this.Control.LpBase.CombineDataPathAndPrepareDirectory(options.Output, LpOptions.DefaultOptionsName);
            if (File.Exists(path) &&
                await this.userInterfaceService.RequestYesOrNo(Hashed.Dialog.ConfirmOverwrite, path) != true)
            {
                return;
            }

            await File.WriteAllBytesAsync(path, utf);
            this.logger.TryGet()?.Log(Hashed.Success.Output, path);
        }
        catch
        {
        }
    }

    public Control Control { get; set; }

    private ILogger<ExportSubcommandOptions> logger;
    private IUserInterfaceService userInterfaceService;
}

public record ExportSubcommandOptionsOptions
{
    [SimpleOption("output", Description = "Output path")]
    public string Output { get; init; } = string.Empty;

    public override string ToString() => $"{this.Output}";
}
