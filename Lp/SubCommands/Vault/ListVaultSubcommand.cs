// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("list-vault")]
public class ListVaultSubcommand : ISimpleCommand
{
    public ListVaultSubcommand(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        var names = this.vaultControl.Root.GetNames();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
