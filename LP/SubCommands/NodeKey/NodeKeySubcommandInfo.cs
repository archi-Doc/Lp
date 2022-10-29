// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("info")]
public class NodeKeySubcommandInfo : ISimpleCommand
{
    public NodeKeySubcommandInfo(ILogger<NodeKeySubcommandInfo> logger, NetBase netBase)
    {
        this.logger = logger;
        this.netBase = netBase;
    }

    public void Run(string[] args)
    {
        var publicKey = this.netBase.NodePublicKey;
        this.logger.TryGet()?.Log(publicKey.ToString());
    }

    private ILogger<NodeKeySubcommandInfo> logger;
    private NetBase netBase;
}
