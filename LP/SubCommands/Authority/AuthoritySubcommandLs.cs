// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class AuthoritySubcommandLs : ISimpleCommandAsync
{
    public AuthoritySubcommandLs(IUserInterfaceService userInterfaceService, Authority authority)
    {
        this.userInterfaceService = userInterfaceService;
        this.authority = authority;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authority.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private Authority authority;
    private IUserInterfaceService userInterfaceService;
}
