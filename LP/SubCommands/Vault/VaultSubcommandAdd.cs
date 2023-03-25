// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("add")]
public class VaultSubcommandAdd : ISimpleCommandAsync<KeyVaultSubcommandAddOptions>
{
    public VaultSubcommandAdd(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(KeyVaultSubcommandAddOptions options, string[] args)
    {
        if (this.vault.Exists(options.Name))
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.AlreadyExists, options.Name));
            return;
        }

        this.vault.Add(options.Name, Encoding.UTF8.GetBytes(options.Text));
        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.AddSuccess, options.Name));
    }

    private Vault vault;
    private IUserInterfaceService userInterfaceService;
}

public record KeyVaultSubcommandAddOptions
{
    [SimpleOption("name", Description = "Name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("text", Description = "Text", Required = true)]
    public string Text { get; init; } = string.Empty;
}
