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
    public KeyVaultSubcommandChangePass(ILoggerSource<KeyVaultSubcommandChangePass> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.KeyVault.ChangePassword);

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

            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.NotMatch);
        }

        var newPassword = await this.Control.UserInterfaceService.RequestPasswordAndConfirm(Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (newPassword == null)
        {
            return;
        }

        this.Control.KeyVault.ChangePassword(currentPassword, newPassword);
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.Changed);
    }

    public Control Control { get; set; }

    private ILoggerSource<KeyVaultSubcommandChangePass> logger;
}
