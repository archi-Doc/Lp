// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Results;
using SimpleCommandLine;
using static SimpleCommandLine.SimpleParser;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("add", Description = "Add storage.")]
public class CrystalStorageSubcommandAdd : ISimpleCommandAsync<CrystalStorageOptionsAdd>
{
    public CrystalStorageSubcommandAdd(ILogger<CrystalStorageSubcommandAdd> logger, IUserInterfaceService userInterfaceService, ICrystal crystal, CrystalStorageSubcommandLs crystalDirSubcommandLs)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.crystal = crystal;
        this.CrystalDirSubcommandLs = crystalDirSubcommandLs;
    }

    public async Task RunAsync(CrystalStorageOptionsAdd options, string[] args)
    {
        if (options.Capacity != 0)
        {
            options.capacityInBytes = (long)options.Capacity * 1024 * 1024 * 1024;
        }

        if (string.IsNullOrEmpty(options.Filer) || options.Filer == "local")
        {
            await this.AddLocal(options, args);
        }
        else if (options.Filer == "s3")
        {
            await this.AddLocal(options, args);
        }
        else
        {
            await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Storage.InvalidFiler, options.Filer);
        }
    }

    private async Task AddLocal(CrystalStorageOptionsAdd options, string[] args)
    {
        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Storage.CreateLocal));

        var path = await this.userInterfaceService.RequestString(true, Hashed.Storage.EnterPath);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var result = this.crystal.Storage.AddStorage_SimpleLocal(path, options.capacityInBytes);
        if (result.Result == AddStorageResult.Success)
        {
            this.logger.TryGet()?.Log($"Storage added: {result.Id:x4}");
            this.userInterfaceService.WriteLine();
            await this.CrystalDirSubcommandLs.RunAsync(Array.Empty<string>());
        }
    }

    public CrystalStorageSubcommandLs CrystalDirSubcommandLs { get; private set; }

    private ILogger<CrystalStorageSubcommandAdd> logger;
    private IUserInterfaceService userInterfaceService;
    private ICrystal crystal;
}

public record CrystalStorageOptionsAdd
{
    [SimpleOption("storage", Description = "Storage type (simple)")]
    public string Storage { get; init; } = string.Empty;

    [SimpleOption("filer", Description = "Filer type (local, s3)")]
    public string Filer { get; init; } = string.Empty;

    [SimpleOption("capacity", Description = "Directory capacity in GB")]
    public int Capacity { get; set; } = 0;

    internal long capacityInBytes { get; set; } = CrystalOptions.DefaultDirectoryCapacity;
}
