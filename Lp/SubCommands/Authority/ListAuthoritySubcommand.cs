// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("list-authority")]
public class ListAuthoritySubcommand : ISimpleCommandAsync
{
    public ListAuthoritySubcommand(IUserInterfaceService userInterfaceService, AuthorityControl authorityControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityControl = authorityControl;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authorityControl.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly AuthorityControl authorityControl;
    private readonly IUserInterfaceService userInterfaceService;
}
