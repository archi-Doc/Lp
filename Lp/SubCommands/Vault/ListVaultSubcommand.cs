// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("list-vault")]
public class ListVaultSubcommand : ISimpleCommandAsync
{
    public ListVaultSubcommand(IUserInterfaceService userInterfaceService, VaultControl vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.vault.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly VaultControl vault;
    private readonly IUserInterfaceService userInterfaceService;
}
