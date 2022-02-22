// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using DryIoc;
using LP;
using SimpleCommandLine;
using Tinyhand;
using ZenItz;

namespace LP.Subcommands;

[SimpleCommand("zendir", AcceptUnknownOptionName = true)]
public class ZenDirSubcommand : ISimpleCommandAsync
{
    public static void Register(Container container)
    {
        commandTypes = new Type[]
        {
            typeof(ZenDirSubcommandLs),
            typeof(ZenDirSubcommandAdd),
        };

        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }
    }

    public ZenDirSubcommand(ZenControl control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        if (commandTypes == null)
        {
            return;
        }
        else if (subcommandParser == null)
        {
            subcommandParser ??= new(commandTypes, ZenControl.SubcommandParserOptions);
        }

        await subcommandParser.ParseAndRunAsync(args);
    }

    private static Type[]? commandTypes;
    private static SimpleParser? subcommandParser;

    public ZenControl Control { get; set; }
}
