// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class KeyVaultSubcommandLs : ISimpleCommandAsync
{
    public KeyVaultSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        var names = this.Control.KeyVault.GetNames();
        Console.WriteLine(string.Join(' ', names));
    }

    public Control Control { get; set; }
}
