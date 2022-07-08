// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;
using ZenItz;

namespace ZenItz.Subcommands;

[SimpleCommand("zendir", IsSubcommand = true, Description = "Zen directory subcommand")]
public class ZenDirSubcommand : ISimpleCommandAsync
{
    private static Type[] commandTypes = new[]
    {
        typeof(ZenDirSubcommandLs),
        typeof(ZenDirSubcommandAdd),
    };

    public static void Register(UnitBuilderContext context)
    {
        foreach (var x in commandTypes)
        {
            context.ServiceCollection.AddSingleton(x);
        }
    }

    public ZenDirSubcommand(ZenControl control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        if (this.subcommandParser == null)
        {
            this.subcommandParser ??= new(commandTypes, this.Control.SubcommandParserOptions);
        }

        await this.subcommandParser.ParseAndRunAsync(args);
    }

    private SimpleParser? subcommandParser;

    public ZenControl Control { get; set; }
}
