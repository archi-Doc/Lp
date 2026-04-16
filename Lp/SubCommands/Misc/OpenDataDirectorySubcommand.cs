// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("open-data-directory")]
public class OpenDataDirectorySubcommand : ISimpleCommand
{
    private readonly LpBase lpBase;

    public OpenDataDirectorySubcommand(LpBase lpBase)
    {
        this.lpBase = lpBase;
    }

    public void Run(string[] args)
    {
        try
        {
            Process.Start("explorer.exe", this.lpBase.DataDirectory);
        }
        catch
        {
        }
    }
}
