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
{// flags on name
    public NodeSubcommandAdd(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        foreach (var x in args)
        {
            if (!NodeAddress.TryParse(x, out var nodeAddress))
            {// Could not parse
                Logger.Default.Warning(Hashed.Error.Parse, x);
                continue;
            }

            if (!nodeAddress.IsValid())
            {// Invalid
                Logger.Default.Warning(Hashed.Error.InvalidAddress, x);
                continue;
            }

            if (this.Control.NetControl.EssentialNode.TryAdd(nodeAddress))
            {// Success
                Logger.Default.Information(Hashed.Success.NodeAdded, x);
            }
        }
    }

    public Control Control { get; set; }
}
