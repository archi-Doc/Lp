// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Unit;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("dump", IsSubcommand = true)]
public class DumpSubcommand : ISimpleCommandAsync
{
    public static void Register(UnitBuilderContext context)
    {
        commandTypes = new Type[]
        {
            typeof(Dump.DumpSubcommandInfo),
            typeof(Dump.DumpSubcommandOptions),
        };

        foreach (var x in commandTypes)
        {
            context.AddSingleton(x);
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

        if (args.Length == 0)
        {// dump info
            args = new string[] { "info", };
        }

        await commandParser.ParseAndRunAsync(args);
    }

    private static Type[]? commandTypes;
    private static SimpleParser? commandParser;

    public Control Control { get; set; }
}
