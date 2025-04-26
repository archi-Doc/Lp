// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

public class ChainKey
{
    public enum Kind : byte
    {
        Merger,
        RelayMerger,
        Linker,
    }

    private readonly Seedphrase seedphrase;
    private readonly string originalphrase;

    public ChainKey(Seedphrase seedphrase, string originalphrase)
    {
        this.seedphrase = seedphrase;
        this.originalphrase = originalphrase;
    }

    public SeedKey GetMergerKey()
         => this.GetKey(Kind.Merger);

    private (string SeedPhrase, SeedKey seedKey) GetKey(Kind kind)
    {
        Span<byte> seed = stackalloc byte[Blake3.Size];
        if (!this.seedphrase.TryAlter(this.originalphrase, [(byte)kind], seed))
        {
            return (string.Empty, SeedKey.NewSignature(seed));
        }
    }
}

[SimpleCommand("new-chain-key")]
public class NewChainKeySubcommand : ISimpleCommand<Subcommand.NewKeyOptions>
{
    public enum Kind : byte
    {
        Merger,
        RelayMerger,
        Linker,
    }

    public NewChainKeySubcommand(ILogger<NewChainKeySubcommand> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(Subcommand.NewKeyOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New chain key");

        byte[]? seed;
        var seedphrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(seedphrase))
        {
            seedphrase = this.seedPhrase.Create();
            seed = this.seedPhrase.TryGetSeed(seedphrase);

            this.userInterfaceService.WriteLine($"Seedphrase: {seedphrase}");
        }
        else
        {
            seed = this.seedPhrase.TryGetSeed(seedphrase);
        }

        if (seed == null)
        {
            this.userInterfaceService.WriteLine(Hashed.Seedphrase.Invalid, seedphrase);
            return;
        }

        var chainKey = new ChainKey(this.seedPhrase, seedphrase);

        var key = chainKey.GetMergerKey();
        this.userInterfaceService.WriteLine(key.UnsafeToString());
        // this.logger.TryGet()?.Log(key.GetSignaturePublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}
