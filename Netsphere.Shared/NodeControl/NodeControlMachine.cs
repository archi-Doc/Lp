// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using Netsphere.Interfaces;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Netsphere.Machines;

/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.<br/>
/// 1: Connect and get nodes.<br/>
/// 2: Determine MyStatus.ConnectionType.<br/>
/// 3: Check essential nodes.
/// </summary>
[MachineObject(UseServiceProvider = true)]
public partial class NodeControlMachine : Machine
{
    public NodeControlMachine(ILogger<NodeControlMachine> logger, NetBase netBase, NetControl netControl, NodeControl nodeControl)
        : base()
    {
        this.logger = logger;
        this.netBase = netBase;
        this.netControl = netControl;
        this.nodeControl = nodeControl;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly NetBase netBase;
    private readonly NodeControl nodeControl;

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {//
        if (!this.netControl.NetTerminal.IsActive)
        {
            // return StateResult.Continue;
        }

        if (!this.nodeControl.TryGetLifelineNode(out var netNode))
        {
            return StateResult.Terminate;
        }

        // var node = await this.netControl.NetTerminal.UnsafeGetNetNode(netAddress);
        var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new());
        Console.WriteLine($"{netNode.Address.ToString()} {r.ToString()}");

        this.logger.TryGet(LogLevel.Information)?.Log($"{netNode.Address.ToString()} - {r.Result.ToString()}");
        if (r.Result == NetResult.Success && r.Value is { } value)
        {// Success
            this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Success);
        }
        else
        {
            this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Failure);
        }

        using (var connection = await this.netControl.NetTerminal.Connect(netNode))
        {
            if (connection is not null)
            {
                var service = connection.GetService<INodeControlService>();
                // await this.nodeControl.Integrate(async (x, y) => await service.DifferentiateEssentialNode(x));
            }
        }

        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> First(StateParameter parameter)
    {
        return StateResult.Terminate;
    }
}
