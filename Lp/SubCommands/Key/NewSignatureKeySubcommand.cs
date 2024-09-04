// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Subcommands;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands;

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

        SignaturePrivateKey key;
        var phrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
                key = SignaturePrivateKey.Create(seed);
            }
            else
            {
                key = SignaturePrivateKey.Create();
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

            key = SignaturePrivateKey.Create(seed);
        }

        this.userInterfaceService.WriteLine(key.UnsafeToString());
        this.logger.TryGet()?.Log(key.ToPublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}
