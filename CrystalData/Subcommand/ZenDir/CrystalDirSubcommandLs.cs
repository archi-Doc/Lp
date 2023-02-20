// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("ls", Description = "List crystal directory information.")]
public class CrystalDirSubcommandLs : ISimpleCommandAsync
{
    public CrystalDirSubcommandLs(IConsoleService consoleService, LpCrystal crystal)
    {
        this.consoleService = consoleService;
        this.crystal = crystal;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.crystal.Storage.GetDirectoryInformation();

        foreach (var x in info)
        {
            this.consoleService.WriteLine(x.ToString());
        }
    }

    private IConsoleService consoleService;
    private LpCrystal crystal;
}
