// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("remove-authority")]
public class RemoveAuthoritySubcommand : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public RemoveAuthoritySubcommand(ILogger<RemoveAuthoritySubcommand> logger, AuthorityControl authorityControl, IUserInterfaceService userInterfaceService)
    {
        this.authorityControl = authorityControl;
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions options, string[] args)
    {
        if (!this.authorityControl.Exists(options.AuthorityName))
        {// Not found
            this.logger.TryGet()?.Log(Hashed.Authority.NotFound, options.AuthorityName);
            return;
        }
        else
        {
            if (await this.userInterfaceService.ReadYesNo(Hashed.Authority.RemoveConfirm, options.AuthorityName) != true)
            {
                return;
            }
        }

        var result = this.authorityControl.RemoveAuthority(options.AuthorityName);

        if (result)
        {// Success
            this.logger.TryGet()?.Log(Hashed.Authority.Removed, options.AuthorityName);
        }
        else
        {// Failed
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, options.AuthorityName);
        }
    }

    private readonly AuthorityControl authorityControl;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
