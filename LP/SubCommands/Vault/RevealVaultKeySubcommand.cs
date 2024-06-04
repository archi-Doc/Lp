// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("reveal-vault-key")]
public class RevealVaultKeySubcommand : ISimpleCommandAsync<SimpleVaultOptions>
{
    public RevealVaultKeySubcommand(IConsoleService consoleService, ILogger<RevealVaultKeySubcommand> logger, Vault vault)
    {
        this.consoleService = consoleService;
        this.logger = logger;
        this.vault = vault;
    }

    public async Task RunAsync(SimpleVaultOptions option, string[] args)
    {//
        if (!this.vault.TryGetAndParse<SignaturePrivateKey>(option.Name, out var key))
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Authority.NotAvailable, option.Name);
            return;
        }

        this.consoleService.WriteLine($"{option.Name}: {key.UnsafeToString()}");
    }

    private readonly IConsoleService consoleService;
    private readonly ILogger logger;
    private readonly Vault vault;
}
