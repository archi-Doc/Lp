// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

[SimpleCommand("new-encryption-key")]
public class NewEncryptionKeySubcommand : ISimpleCommand<Subcommand.NewKeyOptions>
{
    public NewEncryptionKeySubcommand(ILogger<NewEncryptionKeySubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(Subcommand.NewKeyOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New encryption key");

        SeedKey key;
        var phrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
                key = SeedKey.New(seed, KeyOrientation.Encryption);
            }
            else
            {
                key = SeedKey.NewEncryption();
            }
        }
        else
        {
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed == null)
            {
                this.userInterfaceService.WriteLine(Hashed.Seedphrase.Invalid, phrase);
                return;
            }

            key = SeedKey.New(seed, KeyOrientation.Encryption);
        }

        this.userInterfaceService.WriteLine(key.UnsafeToString());
        this.logger.TryGet()?.Log(key.GetEncryptionPublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}
