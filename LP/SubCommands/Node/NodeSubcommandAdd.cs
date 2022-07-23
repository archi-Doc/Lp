// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("add")]
public class NodeSubcommandAdd : ISimpleCommand
{
    public NodeSubcommandAdd(ILogger<NodeSubcommandAdd> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public void Run(string[] args)
    {
        foreach (var x in args)
        {
            if (!NodeAddress.TryParse(x, out var nodeAddress))
            {// Could not parse
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Error.Parse, x);
                continue;
            }

            if (!nodeAddress.IsValid())
            {// Invalid
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Error.InvalidAddress, x);
                continue;
            }

            if (this.Control.NetControl.EssentialNode.TryAdd(nodeAddress))
            {// Success
                this.logger.TryGet()?.Log(Hashed.Success.NodeAdded, x);
            }
        }
    }

    public Control Control { get; set; }

    private ILogger<NodeSubcommandAdd> logger;
}
