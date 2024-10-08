﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("remove-vault")]
public class RemoveVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public RemoveVaultSubcommand(IUserInterfaceService userInterfaceService, Lp.Vault vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(SimpleVaultOptions options, string[] args)
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

    private readonly Lp.Vault vault;
    private readonly IUserInterfaceService userInterfaceService;
}
