// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

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

        NodePrivateKey nodeKey;
        var phrase = options.Seedphrase?.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            phrase = this.seedPhrase.Create();
            var seed = this.seedPhrase.TryGetSeed(phrase);
            if (seed is not null)
            {
                this.userInterfaceService.WriteLine($"Seedphrase: {phrase}");
                nodeKey = NodePrivateKey.Create(seed);
            }
            else
            {
                nodeKey = NodePrivateKey.Create();
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

            nodeKey = NodePrivateKey.Create(seed);
        }

        this.userInterfaceService.WriteLine(nodeKey.UnsafeToString());
        this.logger.TryGet()?.Log(nodeKey.ToPublicKey().ToString());
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Seedphrase seedPhrase;
}

public record NodeKeySubcommandNewOptions
{
    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
