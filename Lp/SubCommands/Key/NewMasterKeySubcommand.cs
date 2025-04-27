// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

[SimpleCommand("new-master-key")]
public class NewMasterKeySubcommand : ISimpleCommand<Subcommand.NewKeyOptions>
{
    public NewMasterKeySubcommand(ILogger<NewMasterKeySubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public void Run(Subcommand.NewKeyOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New master key");

        /*byte[]? seed;
        var seedphrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(seedphrase))
        {
            seedphrase = Seedphrase.Create();
            seed = Seedphrase.TryGetSeed(seedphrase);

            this.userInterfaceService.WriteLine($"Seedphrase: {seedphrase}");
        }
        else
        {
            seed = Seedphrase.TryGetSeed(seedphrase);
        }

        if (seed == null)
        {
            this.userInterfaceService.WriteLine(Hashed.Seedphrase.Invalid, seedphrase);
            return;
        }*/

        var masterKey = MasterKey.New();
        this.userInterfaceService.WriteLine(masterKey.ConvertToString());

        var st = masterKey.ConvertToString();
        MasterKey.TryParse(st, out var masterKey2, out var read);

        this.CreateSeedKey(masterKey, MasterKey.Kind.Merger);
        this.CreateSeedKey(masterKey, MasterKey.Kind.RelayMerger);
        this.CreateSeedKey(masterKey, MasterKey.Kind.Linker);
    }

    private void CreateSeedKey(MasterKey masterKey, MasterKey.Kind kind)
    {
        (var seedphrase, var seedKey) = masterKey.CreateSeedKey(kind);
        this.userInterfaceService.WriteLine($"{kind} key:");
        this.userInterfaceService.WriteLine($"{seedphrase}");
        this.userInterfaceService.WriteLine(seedKey.UnsafeToString());
        this.logger.TryGet()?.Log(seedKey.GetSignaturePublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
