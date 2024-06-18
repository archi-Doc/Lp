// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("list-vault")]
public class ListVaultSubcommand : ISimpleCommandAsync
{
    public ListVaultSubcommand(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.vault.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private Vault vault;
    private IUserInterfaceService userInterfaceService;
}
