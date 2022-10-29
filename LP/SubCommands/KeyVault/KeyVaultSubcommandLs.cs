// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class KeyVaultSubcommandLs : ISimpleCommandAsync
{
    public KeyVaultSubcommandLs(IUserInterfaceService userInterfaceService, Vault vault)
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
