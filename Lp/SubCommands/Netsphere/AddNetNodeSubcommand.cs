// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("add-netnode")]
public class AddNetNodeSubcommand : ISimpleCommand
{
    public AddNetNodeSubcommand(ILogger<AddNetNodeSubcommand> logger, NetStats netStats, BigMachine bigMachine)
    {
        this.logger = logger;
        this.netStats = netStats;
        this.bigMachine = bigMachine;
    }

    public void Run(string[] args)
    {
        foreach (var x in args)
        {
            if (!NetNode.TryParseNetNode(this.logger, x, out var node))
            {
                continue;
            }

            this.netStats.NodeControl.TryAddActiveNode(node);
        }

        _ = this.bigMachine.NodeControlMachine.GetOrCreate().RunAsync(); // Immediately run the machine to quickly obtain nodes.
    }

    private readonly ILogger logger;
    private readonly NetStats netStats;
    private readonly BigMachine bigMachine;
}
