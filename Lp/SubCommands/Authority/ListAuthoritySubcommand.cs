// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.Authority;

[SimpleCommand("list-authority")]
public class ListAuthoritySubcommand : ISimpleCommandAsync
{
    public ListAuthoritySubcommand(IUserInterfaceService userInterfaceService, AuthorityVault authorityVault)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authorityVault.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly AuthorityVault authorityVault;
    private readonly IUserInterfaceService userInterfaceService;
}
