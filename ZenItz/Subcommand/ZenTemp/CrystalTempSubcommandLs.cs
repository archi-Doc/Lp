// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("ls", Description = "List crystal directory information.")]
public class CrystalTempSubcommandLs : ISimpleCommandAsync
{
    public CrystalTempSubcommandLs(IConsoleService consoleService, CrystalControl zenControl)
    {
        this.consoleService = consoleService;
        this.zenControl = zenControl;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.zenControl.Crystal.Storage.GetDirectoryInformation();

        foreach (var x in Enumerable.Range(0, 5))
        {
            this.consoleService.WriteLine(x.ToString());
        }
    }

    private IConsoleService consoleService;
    private CrystalControl zenControl;
}
