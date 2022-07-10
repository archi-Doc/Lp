// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Services;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("changepass")]
public class KeyVaultSubcommandChangePass : ISimpleCommandAsync
{
    public KeyVaultSubcommandChangePass(Control control)
    {
        this.Control = control;
    }

    public async Task Run(string[] args)
    {
        Logger.Default.Warning(Hashed.KeyVault.ChangePassword);

        string? currentPassword;
        while (true)
        {
            currentPassword = await this.Control.UserInterfaceService.RequestPassword(Hashed.Dialog.Password.EnterCurrent);
            if (currentPassword == null)
            {
                return;
            }
            else if (this.Control.KeyVault.CheckPassword(currentPassword))
            {// Correct
                break;
            }

            Logger.Default.Warning(Hashed.Dialog.Password.NotMatch);
        }

        var newPassword = await this.Control.UserInterfaceService.RequestPasswordAndConfirm(Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (newPassword == null)
        {
            return;
        }

        this.Control.KeyVault.ChangePassword(currentPassword, newPassword);
        Logger.Default.Warning(Hashed.Dialog.Password.Changed);
    }

    public Control Control { get; set; }
}
