// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("change-vault-password")]
public class ChangeVaultPasswordSubcommand : ISimpleCommandAsync
{
    public ChangeVaultPasswordSubcommand(ILogger<ChangeVaultPasswordSubcommand> logger, IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Vault.ChangePassword);

        string? currentPassword;
        while (true)
        {
            currentPassword = await this.userInterfaceService.ReadPassword(Hashed.Dialog.Password.EnterCurrent);
            if (currentPassword == null)
            {
                return;
            }
            else if (this.vaultControl.Root.PasswordEquals(currentPassword))
            {// Correct
                break;
            }

            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.NotMatch);
        }

        var newPassword = await this.userInterfaceService.ReadPasswordAndConfirm(Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (newPassword == null)
        {
            return;
        }

        this.vaultControl.Root.SetPassword(newPassword);
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.Changed);
    }

    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
