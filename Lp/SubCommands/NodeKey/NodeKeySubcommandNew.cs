// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new")]
public class NodeKeySubcommandNew : ISimpleCommand<NodeKeySubcommandNewOptions>
{
    public NodeKeySubcommandNew(ILogger<NodeKeySubcommandNew> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(NodeKeySubcommandNewOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New node key");

        NodePrivateKey key;
        var phrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
                key = NodePrivateKey.Create(seed);
            }
            else
            {
                key = NodePrivateKey.Create();
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

            key = NodePrivateKey.Create(seed);
        }

        this.userInterfaceService.WriteLine(key.UnsafeToString());
        this.logger.TryGet()?.Log(key.ToPublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}

public record NodeKeySubcommandNewOptions
{
    [SimpleOption("Seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
