// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("new-vault")]
public class NewVaultSubcommand : ISimpleCommand<NewVaultOptions>
{
    public NewVaultSubcommand(ILogger<NewVaultSubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(NewVaultOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New vault key");

        SignaturePrivateKey key;
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

        key = seed is null ? SignaturePrivateKey.Create() : SignaturePrivateKey.Create(seed);

        this.userInterfaceService.WriteLine(key.UnsafeToString());
        this.logger.TryGet()?.Log(key.ToPublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}

public record NewVaultOptions
{
    [SimpleOption("name", Description = "Vault name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("class", Description = "Key class")]
    public KeyClass KeyClass { get; init; }

    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
