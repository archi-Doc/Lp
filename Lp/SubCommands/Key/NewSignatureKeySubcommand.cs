// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

[SimpleCommand("new-signature-key")]
public class NewSignatureKeySubcommand : ISimpleCommand<Subcommand.NewKeyOptions>
{
    public NewSignatureKeySubcommand(ILogger<NewSignatureKeySubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(Subcommand.NewKeyOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New signature key");

        SeedKey key;
        var phrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
                key = SeedKey.New(seed, KeyOrientation.Signature);
            }
            else
            {
                key = SeedKey.NewSignature();
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

            key = SeedKey.New(seed, KeyOrientation.Signature);
        }

        this.userInterfaceService.WriteLine(key.UnsafeToString());
        this.logger.TryGet()?.Log(key.GetSignaturePublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}
