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
    public NodeSubcommandLs(Control control, IUserInterfaceService userInterfaceService)
    {
        this.control = control;
        this.userInterfaceService = userInterfaceService;
    }

    public void Run(string[] args)
    {
        var list = this.control.NetControl.EssentialNode.Dump();
        foreach (var x in list)
        {
            this.userInterfaceService.WriteLine(x);
        }
    }

    private Control control;
    private IUserInterfaceService userInterfaceService;
}
