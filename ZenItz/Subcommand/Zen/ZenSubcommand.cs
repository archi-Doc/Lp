// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using DryIoc;
using LP.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;
using Tinyhand;
using ZenItz;

namespace ZenItz.Subcommand.Zen;

[SimpleCommand("zen", IsSubcommand = true)]
public class ZenSubcommand : ISimpleCommandAsync
{
    private static Type[] commandTypes = new[]
    {
        typeof(ZenSubcommandTemplate),
        typeof(ZenSubcommandLs),
    };

    public static void Register(UnitBuilderContext context)
    {
        foreach (var x in commandTypes)
        {
            context.ServiceCollection.AddSingleton(x);
        }
    }

    public ZenSubcommand(ZenControl control)
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

    public ZenControl Control { get; set; }

    private SimpleParser? subcommandParser;
}
