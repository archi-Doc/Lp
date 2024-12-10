// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
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

    public async Task RunAsync(SimpleVaultOptions options, string[] args)
    {
        if (!this.vaultControl.Root.Contains(options.Name))
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Vault.NoVault, options.Name);
            return;
        }

        if (this.vaultControl.Root.TryGetByteArray(options.Name, out var byteArray, out _))
        {// Byte array
            try
            {
                var st = Encoding.UTF8.GetString(byteArray);
                this.consoleService.WriteLine($"Vault '{options.Name}': {st}");
            }
            catch
            {
            }
        }
        else
        {
            if (this.vaultControl.Root.TryGetObject<SeedKey>(options.Name, out var key, out _))
            {
                this.consoleService.WriteLine($"Vault '{options.Name}': {key.UnsafeToString()}");
            }
        }
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
}
