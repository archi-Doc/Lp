// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class NodeSubcommandLs : ISimpleCommand
{
    public NodeSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var list = this.Control.NetControl.EssentialNode.Dump();
        foreach (var x in list)
        {
            Console.WriteLine(x);
        }
    }

    public Control Control { get; set; }
}
