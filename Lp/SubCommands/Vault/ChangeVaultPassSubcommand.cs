﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("change-vault-pass")]
public class ChangeVaultPassSubcommand : ISimpleCommandAsync
{
    public ChangeVaultPassSubcommand(ILogger<ChangeVaultPassSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.control = control;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Vault.ChangePassword);

        string? currentPassword;
        while (true)
        {
            currentPassword = await this.control.UserInterfaceService.RequestPassword(Hashed.Dialog.Password.EnterCurrent);
            if (currentPassword == null)
            {
                return;
            }
            else if (this.control.Vault.CheckPassword(currentPassword))
            {// Correct
                break;
            }

            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.NotMatch);
        }

        var newPassword = await this.control.UserInterfaceService.RequestPasswordAndConfirm(Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (newPassword == null)
        {
            return;
        }

        this.control.Vault.ChangePassword(currentPassword, newPassword);
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.Changed);
    }

    private readonly Control control;
    private readonly ILogger logger;
}
