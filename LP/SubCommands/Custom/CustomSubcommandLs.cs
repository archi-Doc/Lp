// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class CustomSubcommandLs : ISimpleCommandAsync
{
    public CustomSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.Control.Vault.GetNames(CustomizedCommand.Prefix).Select(x => x.Substring(CustomizedCommand.Prefix.Length)).ToArray();
        Console.WriteLine(string.Join(' ', names));
    }

    public Control Control { get; set; }
}
