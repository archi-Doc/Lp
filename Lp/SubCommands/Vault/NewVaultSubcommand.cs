// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Globalization;
using System.Text;
using Lp.Services;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

[SimpleCommand("new-vault")]
public class NewVaultSubcommand : ISimpleCommand<NewVaultOptions>
{
    public NewVaultSubcommand(ILogger<NewVaultSubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase, VaultControl vaultControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
        this.vaultControl = vaultControl;
    }

    public void Run(NewVaultOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"New vault key");

        if (!string.IsNullOrEmpty(options.SecretKey))
        {// From private key
            this.ParsePrivateKey(options);
            return;
        }

        if (options.Kind == VaultKind.String)
        {
            this.AddByteArray(options.Name, Encoding.UTF8.GetBytes("test"));
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

        if (options.Kind == VaultKind.EncryptionKey)
        {
            var key = seed is null ? SeedKey.NewEncryption() : SeedKey.NewEncryption(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(key.GetEncryptionPublicKey().ToString());
            this.AddObject(options.Name, key);
        }
        else if (options.Kind == VaultKind.SignatureKey)
        {
            var key = seed is null ? SeedKey.NewSignature() : SeedKey.NewSignature(seed);
            this.userInterfaceService.WriteLine(key.UnsafeToString());
            this.logger.TryGet()?.Log(key.GetSignaturePublicKey().ToString());
            this.AddObject(options.Name, key);
        }
    }

    private void ParsePrivateKey(NewVaultOptions options)
    {
        if ((options.Kind == VaultKind.EncryptionKey || options.Kind == VaultKind.SignatureKey) &&
            SeedKey.TryParse(options.SecretKey, out var seedKey, out _))
        {
            this.userInterfaceService.WriteLine(seedKey.UnsafeToString());
            this.AddObject(options.Name, seedKey);
        }
        else
        {
            this.userInterfaceService.WriteLine(Hashed.Error.InvalidPrivateKey);
        }
    }

    private void AddByteArray(string name, byte[] data)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!this.vaultControl.Root.TryAddByteArray(name, data, out _))
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Vault.AlreadyExists, name);
            }
        }
    }

    private void AddObject(string name, ITinyhandSerializable data)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!this.vaultControl.Root.TryAddObject(name, data, out _))
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Vault.AlreadyExists, name);
            }
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
    private readonly VaultControl vaultControl;
}

public enum VaultKind
{
    String,
    EncryptionKey,
    SignatureKey,
}

public record NewVaultOptions
{
    [SimpleOption("Name", Description = "Vault name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Kind", Description = "Kind [string, encryption, signature]")]
    public VaultKind Kind { get; init; } = VaultKind.SignatureKey;

    [SimpleOption("Seed", Description = "Seedphrase from which the vault is created")]
    public string? Seedphrase { get; init; }

    [SimpleOption("SecretKey", Description = "Secret key from which the vault is created")]
    public string? SecretKey { get; init; }
}
