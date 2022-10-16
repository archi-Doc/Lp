// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("info")]
public class NodeSubcommandInfo : ISimpleCommand
{
    public NodeSubcommandInfo(ILogger<NodeSubcommandInfo> logger, NetStatus netStatus)
    {
        this.logger = logger;
        this.netStatus = netStatus;
    }

    public void Run(string[] args)
    {
        var nodeInformation = this.netStatus.GetMyNodeInformation(false);
        var st = nodeInformation.ToString();
        this.logger.TryGet()?.Log(st);

        /*if (NodeInformation.TryParse(st, out var n))
        {
            this.logger.TryGet()?.Log(n.ToString());
        }*/
    }

    private ILogger<NodeSubcommandInfo> logger;
    private NetStatus netStatus;
}
