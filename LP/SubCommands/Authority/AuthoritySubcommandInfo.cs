// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("info")]
public class AuthoritySubcommandInfo : ISimpleCommandAsync<AuthoritySubcommandNameOptions>
{
    public AuthoritySubcommandInfo(ILogger<AuthoritySubcommandInfo> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(AuthoritySubcommandNameOptions option, string[] args)
    {
        var result = this.Control.Authority.TryGetInterface(option.Name, out var authorityInterface);
        if (result == AuthorityResult.Success)
        {
            (result, var info) = await authorityInterface.TryGetInfo();
            if (result == AuthorityResult.Success && info != null)
            {
                this.logger.TryGet()?.Log(option.Name);
                this.logger.TryGet()?.Log(info.ToString());
            }
        }
        else if (result == AuthorityResult.NotFound)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotFound, option.Name);
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandInfo> logger;
}
