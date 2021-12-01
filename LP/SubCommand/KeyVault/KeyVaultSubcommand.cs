// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using DryIoc;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("keyvault", AcceptUnknownOptionName = true)]
public class KeyVaultSubcommand : ISimpleCommandAsync
{
    public static void Register(Container container)
    {
        commandTypes = new Type[]
        {
            typeof(KeyVaultSubcommandNew),
        };

        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
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
