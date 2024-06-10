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
    protected async Task<StateResult> CheckLifelineNode(StateParameter parameter)
    {
        if (!this.netControl.NetTerminal.IsActive)
        {
            return StateResult.Continue;
        }

        while (!this.CancellationToken.IsCancellationRequested)
        {
            if (this.nodeControl.CountOnline >= NodeControl.SufficientOnlineNodes)
            {// KeepOnlineNode
                this.ChangeState(State.CheckStatus, true);
                return StateResult.Continue;
            }

            if (!this.nodeControl.TryGetLifelineNode(out var netNode))
            {// No lifeline node
                this.ChangeState(State.CheckStatus, true);
                return StateResult.Continue;
            }

            // var node = await this.netControl.NetTerminal.UnsafeGetNetNode(netAddress);
            var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new(), 0, this.CancellationToken);

            this.logger.TryGet(LogLevel.Information)?.Log($"{netNode.Address.ToString()} - {r.Result.ToString()}");
            if (r.Result == NetResult.Success && r.Value is { } value)
            {// Success
                this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Success);
                this.netControl.NetStats.ReportAddress(value.Address);
            }
            else
            {
                this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Failure);
            }

            // Integrate online nodes.
            /*using (var connection = await this.netControl.NetTerminal.Connect(netNode))
            {
                if (connection is not null)
                {
                    var service = connection.GetService<INodeControlService>();
                    await this.nodeControl.Integrate(async (x, y) => await service.DifferentiateEssentialNode(x));
                }
            }*/
        }

        return StateResult.Terminate;
    }

    [StateMethod(1)]
    protected async Task<StateResult> CheckStatus(StateParameter parameter)
    {// KeepOnlineNode
        if (this.nodeControl.CountOnline == 0)
        {// No online node
            this.logger.TryGet(LogLevel.Fatal)?.Log("There are no online nodes. Please check your network connection and add nodes to node_list.");
            return StateResult.Terminate;
        }

        this.ShowStatus();

        return StateResult.Terminate;
    }

    [StateMethod(2)]
    protected async Task<StateResult> KeepOnlineNode(StateParameter parameter)
    {
        // Online -> Lifeline
        // Lifeline offline -> Remove

        return StateResult.Terminate;
    }

    private void ShowStatus()
    {
        this.logger.TryGet()?.Log($"Lifeline online/offline: {this.nodeControl.CountLinfelineOnline}/{this.nodeControl.CountLinfelineOffline}, Online: {this.nodeControl.CountOnline}");
    }
}
