// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("new-vault")]
public class NewVaultSubcommand : ISimpleCommand<NewVaultOptions>
{
    public NewVaultSubcommand(ILogger<NewVaultSubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase, Vault vault)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
        this.vault = vault;
    }

    public void Run(NewVaultOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"New vault key");

        var phrase = options.Seedphrase?.Trim();
        byte[]? seed;
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
            }
        }
        else
        {
            seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed == null)
            {
                this.userInterfaceService.WriteLine(Hashed.Seedphrase.Invalid, phrase);
                return;
            }
        }

        var prefix = options.KeyClass.ToString() + (string.IsNullOrEmpty(options.Name) ? " Temporary: " : $" {options.Name}: ");
        if (options.KeyClass == KeyClass.Signature)
        {
            var key = seed is null ? SignaturePrivateKey.Create() : SignaturePrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());

            if (!string.IsNullOrEmpty(options.Name))
            {
                this.vault.SerializeAndTryAdd(options.Name, key);
            }
        }
        else if (options.KeyClass == KeyClass.NodeEncryption)
        {
            var key = seed is null ? NodePrivateKey.Create() : NodePrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());

            if (!string.IsNullOrEmpty(options.Name))
            {
                this.vault.SerializeAndTryAdd(options.Name, key);
            }
        }
        else
        {
            var key = seed is null ? EncryptionPrivateKey.Create() : EncryptionPrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());

            if (!string.IsNullOrEmpty(options.Name))
            {
                this.vault.SerializeAndTryAdd(options.Name, key);
            }
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
    private readonly Vault vault;
}

public record NewVaultOptions
{
    [SimpleOption("name", Description = "Vault name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("class", Description = "Key class")]
    public KeyClass KeyClass { get; init; } = KeyClass.Signature;

    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
