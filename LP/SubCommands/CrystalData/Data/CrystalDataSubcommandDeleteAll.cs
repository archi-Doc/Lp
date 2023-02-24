// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("delete-all", Description = "Delete all crystal data.")]
public class CrystalDataSubcommandDeleteAll : ISimpleCommandAsync
{
    public CrystalDataSubcommandDeleteAll(IConsoleService consoleService, ICrystal crystal)
    {
        this.consoleService = consoleService;
        this.crystal = crystal;
    }

    public async Task RunAsync(string[] args)
    {
        await this.crystal.StopAsync(new(RemoveAll: true));
        await this.crystal.StartAsync(new());

        this.consoleService.WriteLine("Deleted");
    }

    private IConsoleService consoleService;
    private ICrystal crystal;
}
