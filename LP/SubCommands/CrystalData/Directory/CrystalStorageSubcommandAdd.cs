// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Results;
using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("add", Description = "Add storage.")]
public class CrystalStorageSubcommandAdd : ISimpleCommandAsync<CrystalDirOptionsAdd>
{
    public CrystalStorageSubcommandAdd(ILogger<CrystalStorageSubcommandAdd> logger, IConsoleService consoleService, CrystalControl crystalControl, ICrystal crystal, CrystalStorageSubcommandLs crystalDirSubcommandLs)
    {
        this.logger = logger;
        this.consoleService = consoleService;
        this.crystalControl = crystalControl;
        this.crystal = crystal;
        this.CrystalDirSubcommandLs = crystalDirSubcommandLs;
    }

    public async Task RunAsync(CrystalDirOptionsAdd option, string[] args)
    {
        long cap = CrystalOptions.DefaultDirectoryCapacity;
        if (option.Capacity != 0)
        {
            cap = (long)option.Capacity * 1024 * 1024 * 1024;
        }

        // await this.crystal.Pause();
        var result = this.crystal.Storage.AddStorage(option.Path, capacity: cap);
        // this.crystal.Restart();

        if (result == AddStorageResult.Success)
        {
            this.logger.TryGet()?.Log($"Directory added: {option.Path}");
            this.consoleService.WriteLine();
            await this.CrystalDirSubcommandLs.RunAsync(Array.Empty<string>());
            // await this.crystal.SimpleParser.ParseAndRunAsync("cdir ls");
        }
    }

    public CrystalStorageSubcommandLs CrystalDirSubcommandLs { get; private set; }

    private ILogger<CrystalStorageSubcommandAdd> logger;
    private IConsoleService consoleService;
    private CrystalControl crystalControl;
    private ICrystal crystal;
}

public record CrystalDirOptionsAdd
{
    [SimpleOption("path", Required = true, Description = "Directory path")]
    public string Path { get; init; } = string.Empty;

    [SimpleOption("capacity", Description = "Directory capacity in GB")]
    public int Capacity { get; init; } = 0;
}
