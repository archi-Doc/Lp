// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("keyvault")]
public class KeyVaultSubcommand : ISimpleCommandAsync
{
    public KeyVaultSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
    }

    public Control Control { get; set; }
}
