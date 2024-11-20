// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("remove-vault")]
public class RemoveVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public RemoveVaultSubcommand(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(SimpleVaultOptions options, string[] args)
    {
        if (this.vaultControl.Root.Remove(options.Name))
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.DeleteSuccess, options.Name));
        }
        else
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.NotFound, options.Name));
        }
    }

    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
