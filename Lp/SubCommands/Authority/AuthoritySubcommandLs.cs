// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class AuthoritySubcommandLs : ISimpleCommandAsync
{
    public AuthoritySubcommandLs(IUserInterfaceService userInterfaceService, AuthorityVault authorityVault)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authorityVault.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private AuthorityVault authorityVault;
    private IUserInterfaceService userInterfaceService;
}
