// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("show-authority")]
public class ShowAuthoritySubcommand : ISimpleCommand<AuthoritySubcommandNameOptions>
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;

    public ShowAuthoritySubcommand(IUserInterfaceService userInterfaceService, ILogger<ShowAuthoritySubcommand> logger, AuthorityControl authorityControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.authorityControl = authorityControl;
    }

    public async Task Execute(AuthoritySubcommandNameOptions option, string[] args, CancellationToken cancellationToken)
    {
        if (!this.authorityControl.Exists(option.AuthorityName))
        {// Not found
            this.userInterfaceService.WriteLine(Hashed.Authority.NotFound, option.AuthorityName);
            return;
        }

        var authority = await this.authorityControl.GetAuthority(option.AuthorityName);
        if (authority is not null)
        {
            this.userInterfaceService.WriteLine($"{authority.ToString()}");
            // this.userInterfaceService.WriteLine($"{authority.GetSeedKey().GetSignaturePublicKey().ToString()}");
        }
        else
        {
            this.logger.GetWriter(LogLevel.Warning)?.Write(Hashed.Authority.NotAvailable, option.AuthorityName);
        }
    }
}
