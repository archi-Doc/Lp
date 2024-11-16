// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("new-vault")]
public class NewVaultSubcommand : ISimpleCommand<NewVaultOptions>
{
    public NewVaultSubcommand(ILogger<NewVaultSubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase, Lp.VaultControl vault)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
        this.vault = vault;
    }

    public void Run(NewVaultOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"New vault key");

        if (!string.IsNullOrEmpty(options.PrivateKey))
        {
            this.ParsePrivateKey(options);
            return;
        }

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

        var prefix = GetPrefix(options.KeyClass, options);
        if (options.KeyClass == KeyClass.Signature)
        {
            var key = seed is null ? SignaturePrivateKey.Create() : SignaturePrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());
            this.AddVault(options.Name, key);
        }
        else if (options.KeyClass == KeyClass.NodeEncryption)
        {
            var key = seed is null ? NodePrivateKey.Create() : NodePrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());
            this.AddVault(options.Name, key);
        }
        else
        {
            var key = seed is null ? EncryptionPrivateKey.Create() : EncryptionPrivateKey.Create(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(prefix + key.ToPublicKey().ToString());
            this.AddVault(options.Name, key);
        }
    }

    private static string GetPrefix(KeyClass keyClass, NewVaultOptions options)
        => keyClass.ToString() + (string.IsNullOrEmpty(options.Name) ? " Temporary: " : $" {options.Name}: ");

    private void ParsePrivateKey(NewVaultOptions options)
    {
        if (SignaturePrivateKey.TryParse(options.PrivateKey, out var signaturePrivateKey))
        {
            this.userInterfaceService.WriteLine(signaturePrivateKey.UnsafeToString());
            var prefix = GetPrefix(signaturePrivateKey.KeyClass, options);
            this.logger.TryGet()?.Log(prefix + signaturePrivateKey.ToPublicKey().ToString());
            this.AddVault(options.Name, signaturePrivateKey);
        }
        else if (EncryptionPrivateKey.TryParse(options.PrivateKey, out var encryptionPrivateKey))
        {
            this.userInterfaceService.WriteLine(encryptionPrivateKey.UnsafeToString());
            var prefix = GetPrefix(encryptionPrivateKey.KeyClass, options);
            this.logger.TryGet()?.Log(prefix + encryptionPrivateKey.ToPublicKey().ToString());
            this.AddVault(options.Name, encryptionPrivateKey);
        }
        else if (NodePrivateKey.TryParse(options.PrivateKey, out var nodePrivateKey))
        {
            this.userInterfaceService.WriteLine(nodePrivateKey.UnsafeToString());
            var prefix = GetPrefix(nodePrivateKey.KeyClass, options);
            this.logger.TryGet()?.Log(prefix + nodePrivateKey.ToPublicKey().ToString());
            this.AddVault(options.Name, nodePrivateKey);
        }
        else
        {
            this.userInterfaceService.WriteLine(Hashed.Error.InvalidPrivateKey);
        }
    }

    private void AddVault<T>(string name, T data)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!this.vault.SerializeAndTryAdd(name, data))
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Vault.AlreadyExists, name);
            }
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
    private readonly Lp.VaultControl vault;
}

public record NewVaultOptions
{
    [SimpleOption("Name", Description = "Vault name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Class", Description = "Key class [Encryption, Signature, NodeEncryption]")]
    public KeyClass KeyClass { get; init; } = KeyClass.Signature;

    [SimpleOption("Seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }

    [SimpleOption("PrivateKey", Description = "PrivateKey")]
    public string? PrivateKey { get; init; }
}
