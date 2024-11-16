// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("list-authority")]
public class ListAuthoritySubcommand : ISimpleCommandAsync
{
    public ListAuthoritySubcommand(IUserInterfaceService userInterfaceService, AuthorityControl authorityVault)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authorityVault.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly AuthorityControl authorityVault;
    private readonly IUserInterfaceService userInterfaceService;
}
