// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("ls", Description = "List crystal data information.")]
public class CrystalDataSubcommandLs : ISimpleCommandAsync
{
    public CrystalDataSubcommandLs(IConsoleService consoleService/*, IBigCrystal crystal*/)
    {
        this.consoleService = consoleService;
        // this.crystal = crystal;
    }

    public async Task RunAsync(string[] args)
    {
        /*var info = this.crystal.GroupStorage.GetInformation();

        if (info.Length == 0)
        {
            this.consoleService.WriteLine("No storage");
            return;
        }

        foreach (var x in info)
        {
            this.consoleService.WriteLine(x);
        }*/
    }

    private IConsoleService consoleService;
    // private IBigCrystal crystal;
}
