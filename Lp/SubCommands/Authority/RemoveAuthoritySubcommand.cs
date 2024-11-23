// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
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

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        if (!this.authorityControl.Exists(option.AuthorityName))
        {// Not found
            this.logger.TryGet()?.Log(Hashed.Authority.NotFound, option.AuthorityName);
            return;
        }
        else
        {
            if (await this.userInterfaceService.RequestYesOrNo(Hashed.Authority.RemoveConfirm, option.AuthorityName) != true)
            {
                return;
            }
        }

        var result = this.authorityControl.RemoveAuthority(option.AuthorityName);

        if (result)
        {// Success
            this.logger.TryGet()?.Log(Hashed.Authority.Removed, option.AuthorityName);
        }
        else
        {// Failed
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, option.AuthorityName);
        }
    }

    private readonly AuthorityControl authorityControl;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}

public record AuthoritySubcommandNameOptions
{
    [SimpleOption("Name", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
