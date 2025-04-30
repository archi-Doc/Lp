// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;
using static Lp.Subcommands.KeyCommand.NewMasterKeySubcommand;

namespace Lp.Subcommands.KeyCommand;

[SimpleCommand("new-master-key")]
public class NewMasterKeySubcommand : ISimpleCommand<Options>
{
    public record Options
    {
        [SimpleOption("Seed", Description = "Base64Url encoded seed byte array")]
        public string? Seed { get; init; }
    }

    public NewMasterKeySubcommand(ILogger<NewMasterKeySubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public void Run(Options options, string[] args)
    {
        this.logger.TryGet()?.Log("New master key");

        MasterKey? masterKey = default;
        var st = options.Seed?.Trim();
        if (!string.IsNullOrEmpty(st))
        {
            if (!MasterKey.TryParse(st, out masterKey, out _))
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.InvalidSeed);
                return;
            }
        }

        if (masterKey is null)
        {
            masterKey = MasterKey.New();
            this.userInterfaceService.WriteLine(masterKey.ConvertToString());
        }

        // st = masterKey.ConvertToString();
        // MasterKey.TryParse(st, out var masterKey2, out var read);

        this.CreateSeedKey(masterKey, MasterKey.Kind.Node);
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
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
