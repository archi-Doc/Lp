// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("delete-vault")]
public class DeleteVaultSubcommand : ISimpleCommandAsync<DeleteVaultOptions>
{
    public DeleteVaultSubcommand(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(DeleteVaultOptions options, string[] args)
    {
        if (this.vault.Remove(options.Name))
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.DeleteSuccess, options.Name));
        }
        else
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.NotFound, options.Name));
        }
    }

    private Vault vault;
    private IUserInterfaceService userInterfaceService;
}

public record DeleteVaultOptions
{
    [SimpleOption("name", Description = "Name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
