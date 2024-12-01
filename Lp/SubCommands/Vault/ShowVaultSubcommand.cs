// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("show-vault")]
public class ShowVaultSubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public ShowVaultSubcommand(IConsoleService consoleService, ILogger<ShowVaultSubcommand> logger, VaultControl vaultControl)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(SimpleVaultOptions option, string[] args)
    {
        /*if (this.vaultControl.Root.TryGetObject<SignaturePrivateKey>(option.Name, out var key, out _))
        {
            this.consoleService.WriteLine($"{key.KeyClass.ToString()} {option.Name}: {key.UnsafeToString()}");
            return;
        }*/

        this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Vault.NotAvailable, option.Name);
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
}
