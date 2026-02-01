// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("change-authority-password")]
public class ChangeAuthorityPasswordSubcommand : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public ChangeAuthorityPasswordSubcommand(ILogger<ChangeAuthorityPasswordSubcommand> logger, IUserInterfaceService userInterfaceService, AuthorityControl authorityControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.authorityControl = authorityControl;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions options, string[] args)
    {
        if (!this.authorityControl.Exists(options.AuthorityName))
        {// Not found
            this.logger.TryGet()?.Log(Hashed.Authority.NotFound, options.AuthorityName);
            return;
        }

        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.ChangePassword);

        Authority? authority;
        while (true)
        {
            var currentPassword = await this.userInterfaceService.ReadPassword(Hashed.Dialog.Password.EnterCurrent);
            if (currentPassword == null)
            {
                return;
            }

            authority = await this.authorityControl.GetAuthority(options.AuthorityName, currentPassword);
            if (authority is not null)
            {
                break;
            }

            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.NotMatch);
        }

        var newPassword = await this.userInterfaceService.RequestPasswordAndConfirm(Hashed.Dialog.Password.EnterNew, Hashed.Dialog.Password.Confirm);
        if (newPassword == null)
        {
            return;
        }

        authority.Vault?.SetPassword(newPassword);
        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Dialog.Password.Changed);
    }

    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
    private readonly IUserInterfaceService userInterfaceService;
}
