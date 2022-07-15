// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("ls")]
public class NodeSubcommandLs : ISimpleCommand
{
    public NodeSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        this.Control.NetControl.EssentialNode.Dump();
    }

    public Control Control { get; set; }
}
