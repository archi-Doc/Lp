// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;
using ZenItz;

namespace CrystalData.Subcommands;

[SimpleCommand("ls", Description = "List zen directory information.")]
public class ZenTempSubcommandLs : ISimpleCommandAsync
{
    public ZenTempSubcommandLs(IConsoleService consoleService, ZenControl zenControl)
    {
        this.consoleService = consoleService;
        this.zenControl = zenControl;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.zenControl.Zen.Storage.GetDirectoryInformation();

        foreach (var x in Enumerable.Range(0, 5))
        {
            this.consoleService.WriteLine(x.ToString());
        }
    }

    private IConsoleService consoleService;
    private ZenControl zenControl;
}
