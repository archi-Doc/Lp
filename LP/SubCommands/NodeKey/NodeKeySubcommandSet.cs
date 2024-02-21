// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("set")]
public class NodeKeySubcommandSet : ISimpleCommand<NodeKeySubcommandSetOptions>
{
    public NodeKeySubcommandSet(ILogger<NodeKeySubcommandNew> logger, NetBase netBase, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.seedPhrase = seedPhrase;
    }

    public void Run(NodeKeySubcommandSetOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("Set node key");

        NodePrivateKey.TryParse(options.Key, out var nodeKey);
        if (nodeKey == null)
        {
            var seed = this.seedPhrase.TryGetSeed(options.Key);
            if (seed != null)
            {
                nodeKey = NodePrivateKey.Create(seed);
            }
        }

        if (nodeKey == null)
        {
            this.logger.TryGet()?.Log(Hashed.Subcommands.NodeKey.InvalidKey, options.Key);
            return;
        }

        this.netBase.SetNodePrivateKey(nodeKey);
        this.logger.TryGet()?.Log(Hashed.Subcommands.NodeKey.Changed, nodeKey.ToPublicKey().ToString());
    }

    private ILogger<NodeKeySubcommandNew> logger;
    private NetBase netBase;
    private Seedphrase seedPhrase;
}

public record NodeKeySubcommandSetOptions
{
    [SimpleOption("key", Description = "Node private key or seedphrase", Required = true)]
    public string Key { get; init; } = string.Empty;
}
