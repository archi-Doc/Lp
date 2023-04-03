﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("ls", Description = "List storages.")]
public class CrystalStorageSubcommandLs : ISimpleCommandAsync
{
    public CrystalStorageSubcommandLs(IConsoleService consoleService, IBigCrystal crystal)
    {
        this.consoleService = consoleService;
        this.crystal = crystal;
    }

    public async Task RunAsync(string[] args)
    {
        var info = this.crystal.StorageGroup.GetInformation();

        foreach (var x in info)
        {
            this.consoleService.WriteLine(x);
        }
    }

    private IConsoleService consoleService;
    private IBigCrystal crystal;
}
