// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class AuthoritySubcommandLs : ISimpleCommandAsync
{
    public AuthoritySubcommandLs(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.Control.Vault.GetNames("Authority\\");
        Console.WriteLine(string.Join(' ', names));
    }

    public Control Control { get; set; }
}
