// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("ls", Description = "List crystal directory information.")]
public class CrystalDirSubcommandLs : ISimpleCommandAsync
{
    public CrystalDirSubcommandLs(IConsoleService consoleService, CrystalControl zenControl)
    {
        this.consoleService = consoleService;
        this.zenControl = zenControl;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.zenControl.Crystal.Storage.GetDirectoryInformation();

        foreach (var x in info)
        {
            this.consoleService.WriteLine(x.ToString());
        }
    }

    private IConsoleService consoleService;
    private CrystalControl zenControl;
}
