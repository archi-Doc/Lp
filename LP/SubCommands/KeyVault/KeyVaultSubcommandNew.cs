// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("new")]
public class KeyVaultSubcommandNew : ISimpleCommandAsync<KeyVaultOptionsNew>
{
    public KeyVaultSubcommandNew(Control control)
    {
        this.Control = control;
    }

    public async Task Run(KeyVaultOptionsNew option, string[] args)
    {
        Console.WriteLine($"KeyVault New: {option.Name}");
    }

    public Control Control { get; set; }
}

public record KeyVaultOptionsNew
{
    [SimpleOption("name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
