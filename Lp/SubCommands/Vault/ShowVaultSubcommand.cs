﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("show-vault")]
public class ShowVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public ShowVaultSubcommand(IConsoleService consoleService, ILogger<ShowVaultSubcommand> logger, VaultControl vault)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.vaultControl = vault;
    }

    public async Task RunAsync(SimpleVaultOptions option, string[] args)
    {
        if (this.vaultControl.TryGetAndDeserialize<SignaturePrivateKey>(option.Name, out var key))
        {
            this.consoleService.WriteLine($"{key.KeyClass.ToString()} {option.Name}: {key.UnsafeToString()}");
            return;
        }

        if (this.vaultControl.TryGetAndDeserialize<EncryptionPrivateKey>(option.Name, out var key2))
        {
            this.consoleService.WriteLine($"{key2.KeyClass.ToString()} {option.Name}: {key2.UnsafeToString()}");
            return;
        }

        if (this.vaultControl.TryGetAndDeserialize<NodePrivateKey>(option.Name, out var key3))
        {
            this.consoleService.WriteLine($"{key3.KeyClass.ToString()} {option.Name}: {key3.UnsafeToString()}");
            return;
        }

        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Vault.NotAvailable, option.Name);
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
}
