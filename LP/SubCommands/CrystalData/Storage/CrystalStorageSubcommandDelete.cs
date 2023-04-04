// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("delete", Description = "Delete storage.")]
public class CrystalStorageSubcommandDelete : ISimpleCommandAsync<CrystalStorageOptionsDelete>
{
    public CrystalStorageSubcommandDelete(ILogger<CrystalStorageSubcommandAdd> logger, IUserInterfaceService userInterfaceService, CrystalControl crystalControl, IBigCrystal crystal, CrystalStorageSubcommandLs crystalDirSubcommandLs)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.crystalControl = crystalControl;
        this.crystal = crystal;
        this.CrystalDirSubcommandLs = crystalDirSubcommandLs;
    }

    public async Task RunAsync(CrystalStorageOptionsDelete options, string[] args)
    {
        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Subcommands.DevStage));

        ushort.TryParse(options.Id, System.Globalization.NumberStyles.HexNumber, null, out var id);
        if (!this.crystal.StorageGroup.CheckStorageId(id))
        {
            await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Storage.InvalidId, options.Id);
            return;
        }

        if (await this.userInterfaceService.RequestYesOrNo(Hashed.Storage.DeleteConfirm) != true)
        {
            return;
        }

        if (this.crystal.StorageGroup.DeleteStorage(id))
        {
            await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Storage.Deleted, options.Id);
            await this.CrystalDirSubcommandLs.RunAsync(Array.Empty<string>());
        }
    }

    public CrystalStorageSubcommandLs CrystalDirSubcommandLs { get; private set; }

    private ILogger<CrystalStorageSubcommandAdd> logger;
    private IUserInterfaceService userInterfaceService;
    private CrystalControl crystalControl;
    private IBigCrystal crystal;
}

public record CrystalStorageOptionsDelete
{
    [SimpleOption("id", Required = true, Description = "Storage id")]
    public string Id { get; init; } = string.Empty;
}
