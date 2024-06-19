// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("remove-vault")]
public class RemoveVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public RemoveVaultSubcommand(IUserInterfaceService userInterfaceService, Vault vault)
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

    private Vault vault;
    private IUserInterfaceService userInterfaceService;
}
