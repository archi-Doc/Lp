// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using DryIoc;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("template", IsSubcommand = true)]
public class TemplateSubcommand : ISimpleCommandAsync
{
    public static void Register(Container container)
    {
        commandTypes = new Type[]
        {
            typeof(TemplateSubcommandNew),
        };

        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }
    }

    public TemplateSubcommand(Control control)
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
            subcommandParser ??= new(commandTypes, Control.SubcommandParserOptions);
        }

        await subcommandParser.ParseAndRunAsync(args);
    }

    private static Type[]? commandTypes;
    private static SimpleParser? subcommandParser;

    public Control Control { get; set; }
}
