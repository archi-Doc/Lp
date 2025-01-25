// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands;

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
            if (!NetNode.TryParseNetNode(this.logger, x, out var node))
            {
                continue;
            }

            this.Control.NetControl.NetStats.NodeControl.TryAddActiveNode(node);//
        }
    }

    public Control Control { get; set; }

    private ILogger<NodeSubcommandAdd> logger;
}
