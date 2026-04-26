// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("change-vault-password")]
public class ChangeVaultPasswordSubcommand : ISimpleCommand
{
    public ChangeVaultPasswordSubcommand(ILogger<ChangeVaultPasswordSubcommand> logger, IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter(LogLevel.Warning)?.Write(Hashed.Vault.ChangePassword);

        InputResult result;
        while (true)
        {
            result = await this.userInterfaceService.ReadPassword(true, Hashed.Dialog.Password.EnterCurrent);
            if (!result.IsSuccess)
            {
                return;
            }
            else if (this.vaultControl.Root.PasswordEquals(result.Text))
            {// Correct
                break;
            }

            this.logger.GetWriter(LogLevel.Warning)?.Write(Hashed.Dialog.Password.NotMatch);
        }

        result = await this.userInterfaceService.ReadPasswordAndConfirm(true, Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (!result.IsSuccess)
        {
            return;
        }

        this.vaultControl.Root.SetPassword(result.Text);
        this.logger.GetWriter(LogLevel.Warning)?.Write(Hashed.Dialog.Password.Changed);
    }

    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
