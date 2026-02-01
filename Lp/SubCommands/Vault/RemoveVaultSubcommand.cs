// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.Subcommands.AuthorityCommand;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("remove-vault")]
public class RemoveVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public RemoveVaultSubcommand(ILogger<RemoveAuthoritySubcommand> logger, IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(SimpleVaultOptions options, string[] args)
    {
        if (!this.vaultControl.Root.Contains(options.Name))
        {// Not found
            this.logger.TryGet()?.Log(Hashed.Authority.NotFound, options.Name);
            return;
        }
        else
        {
            if (await this.userInterfaceService.ReadYesNo(Hashed.Vault.RemoveConfirm, options.Name).ConfigureAwait(false)
                != InputResultKind.Success)
            {
                return;
            }
        }

        if (this.vaultControl.Root.Remove(options.Name))
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.Removed, options.Name));
        }
        else
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.NotFound, options.Name));
        }
    }

    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
