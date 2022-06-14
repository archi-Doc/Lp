// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using DryIoc;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("dump", IsSubcommand = true)]
public class DumpSubcommand : ISimpleCommandAsync
{
    public static void Register(Container container)
    {
        commandTypes = new Type[]
        {
            typeof(Dump.DumpSubcommandInfo),
            typeof(Dump.DumpSubcommandOptions),
        };

        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }
    }

    public DumpSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        if (commandTypes == null)
        {
            return;
        }
        else if (commandParser == null)
        {
            commandParser ??= new(commandTypes, Control.SubcommandParserOptions);
        }

        await commandParser.ParseAndRunAsync(args);
    }

    private static Type[]? commandTypes;
    private static SimpleParser? commandParser;

    public Control Control { get; set; }
}
