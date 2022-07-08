// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Unit;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("keyvault", IsSubcommand = true)]
public class KeyVaultSubcommand : ISimpleCommandAsync
{
    public static void Register(UnitBuilderContext context)
    {
        commandTypes = new Type[]
        {
            typeof(KeyVaultSubcommandNew),
        };

        foreach (var x in commandTypes)
        {
            context.AddSingleton(x);
        }
    }

    public KeyVaultSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        if (commandTypes == null)
        {
            return;
        }
        else if (keyVaultParser == null)
        {
            keyVaultParser ??= new(commandTypes, Control.SubcommandParserOptions);
        }

        await keyVaultParser.ParseAndRunAsync(args);
    }

    private static Type[]? commandTypes;
    private static SimpleParser? keyVaultParser;

    public Control Control { get; set; }
}
