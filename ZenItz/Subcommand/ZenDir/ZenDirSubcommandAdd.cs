// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using CrystalData.Results;
using LP;
using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("add", Description = "Add zen directory.")]
public class ZenDirSubcommandAdd : ISimpleCommandAsync<ZenDirOptionsAdd>
{
    public ZenDirSubcommandAdd(ILogger<ZenDirSubcommandAdd> logger, IConsoleService consoleService, CrystalControl zenControl, ZenDirSubcommandLs zenDirSubcommandLs)
    {
        this.logger = logger;
        this.consoleService = consoleService;
        this.zenControl = zenControl;
        this.ZenDirSubcommandLs = zenDirSubcommandLs;
    }

    public async Task RunAsync(ZenDirOptionsAdd option, string[] args)
    {
        long cap = CrystalOptions.DefaultDirectoryCapacity;
        if (option.Capacity != 0)
        {
            cap = (long)option.Capacity * 1024 * 1024 * 1024;
        }

        // await this.zenControl.Zen.Pause();
        var result = this.zenControl.Zen.Storage.AddDirectory(option.Path, capacity: cap);
        // this.zenControl.Zen.Restart();

        if (result == AddDictionaryResult.Success)
        {
            this.logger.TryGet()?.Log($"Directory added: {option.Path}");
            this.consoleService.WriteLine();
            await this.ZenDirSubcommandLs.RunAsync(Array.Empty<string>());
            // await this.ZenControl.SimpleParser.ParseAndRunAsync("zendir ls");
        }
    }

    public ZenDirSubcommandLs ZenDirSubcommandLs { get; private set; }

    private ILogger<ZenDirSubcommandAdd> logger;
    private IConsoleService consoleService;
    private CrystalControl zenControl;
}

public record ZenDirOptionsAdd
{
    [SimpleOption("path", Required = true, Description = "Directory path")]
    public string Path { get; init; } = string.Empty;

    [SimpleOption("capacity", Description = "Directory capacity in GB")]
    public int Capacity { get; init; } = 0;
}
