// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("options")]
public class ExportSubcommandOptions : ISimpleCommandAsync<ExportSubcommandOptionsOptions>
{
    public ExportSubcommandOptions(ILogger<ExportSubcommandOptions> logger, IUserInterfaceService userInterfaceService, LpUnit lpUnit)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.LpUnit = lpUnit;
    }

    public async Task RunAsync(ExportSubcommandOptionsOptions options, string[] args)
    {
        try
        {
            var utf = TinyhandSerializer.SerializeToUtf8(this.LpUnit.LpBase.Options with { OptionsPath = string.Empty, });

            var path = this.LpUnit.LpBase.CombineDataPathAndPrepareDirectory(options.Output, LpOptions.DefaultOptionsName);
            if (File.Exists(path) &&
                await this.userInterfaceService.ReadYesNo(Hashed.Dialog.ConfirmOverwrite, path) != InputResultKind.Success)
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

    public LpUnit LpUnit { get; set; }

    private ILogger<ExportSubcommandOptions> logger;
    private IUserInterfaceService userInterfaceService;
}

public record ExportSubcommandOptionsOptions
{
    [SimpleOption("Output", Description = "Output path")]
    public string Output { get; init; } = string.Empty;

    public override string ToString() => $"{this.Output}";
}
