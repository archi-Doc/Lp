// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class TemplateSubcommandLs : ISimpleCommandAsync
{
    public TemplateSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        Console.WriteLine("Template");
    }

    public Control Control { get; set; }
}
