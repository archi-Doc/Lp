// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("ls", Description = "List crystal directory information.")]
public class CrystalTempSubcommandLs : ISimpleCommandAsync
{
    public CrystalTempSubcommandLs(IConsoleService consoleService, ICrystal crystal)
    {
        this.consoleService = consoleService;
        this.crystal = crystal;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.crystal.Storage.GetDirectoryInformation();

        foreach (var x in Enumerable.Range(0, 5))
        {
            this.consoleService.WriteLine(x.ToString());
        }
    }

    private IConsoleService consoleService;
    private ICrystal crystal;
}
