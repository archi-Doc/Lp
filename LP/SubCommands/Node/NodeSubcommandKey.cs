// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("key")]
public class NodeSubcommandKey : ISimpleCommand<NodeSubcommandKeyOptions>
{
    public NodeSubcommandKey(ILogger<NodeSubcommandKey> logger, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.seedPhrase = seedPhrase;
    }

    public void Run(NodeSubcommandKeyOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New node key");

        NodePrivateKey nodeKey;

        if (string.IsNullOrEmpty(options.Seedphrase))
        {
            nodeKey = NodePrivateKey.Create();
        }
        else
        {
            var seed = this.seedPhrase.TryGetSeed(options.Seedphrase);
            if (seed == null)
            {
                this.logger.TryGet()?.Log(Hashed.Seedphrase.Invalid, options.Seedphrase);
                return;
            }

            nodeKey = NodePrivateKey.Create(seed);
        }

        this.logger.TryGet()?.Log(nodeKey.ToString());
    }

    private ILogger<NodeSubcommandKey> logger;
    private Seedphrase seedPhrase;
}

public record NodeSubcommandKeyOptions
{
    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
