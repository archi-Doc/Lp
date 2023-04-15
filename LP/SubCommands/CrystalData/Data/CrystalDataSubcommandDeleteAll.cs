﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("delete-all", Description = "Delete all crystal data.")]
public class CrystalDataSubcommandDeleteAll : ISimpleCommandAsync
{
    public CrystalDataSubcommandDeleteAll(IConsoleService consoleService, Crystalizer crystal)
    {
        this.consoleService = consoleService;
        this.crystalizer = crystal;
    }

    public async Task RunAsync(string[] args)
    {// tempcode
        await this.crystalizer.DeleteAll();
        await this.crystalizer.PrepareAndLoadAll(new());

        this.consoleService.WriteLine("Deleted");
    }

    private IConsoleService consoleService;
    private Crystalizer crystalizer;
}
