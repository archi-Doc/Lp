// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Lp.Machines;

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
            if (this.nodeControl.HasSufficientOnlineNodes)
            {// KeepOnlineNode
                this.ChangeState(State.CheckStatus, true);
                return StateResult.Continue;
            }

            if (!this.nodeControl.TryGetLifelineNode(out var netNode))
            {// No lifeline node
                this.ChangeState(State.CheckStatus, true);
                return StateResult.Continue;
            }

            if (!netNode.Address.IsValidIpv4AndIpv6)
            {
                this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Failure);
                continue;
            }

            var ipv6Task = this.PingNetNode(netNode, true);
            var ipv4Task = this.PingNetNode(netNode, false);
            var result = await Task.WhenAll(ipv6Task, ipv4Task);

            if (result[0].IsValid)
            {
                if (result[1].IsValid)
                {// Ipv6 available, Ipv4 available
                    this.netControl.NetStats.OutboundPort.Add(result[0].EndPoint!.Port);
                    this.netControl.NetStats.PublicAccess.ReportPortNumber(result[0].EndPoint!.Port);
                }
                else
                {// Ipv6 available, Ipv4 not available
                    this.netControl.NetStats.OutboundPort.Add(result[0].EndPoint!.Port);
                    this.netControl.NetStats.PublicAccess.ReportPortNumber(result[0].EndPoint!.Port);
                }
            }
            else
            {
                if (result[1].IsValid)
                {// Ipv6 not available, Ipv4 available
                    this.netControl.NetStats.OutboundPort.Add(result[1].EndPoint!.Port);
                    this.netControl.NetStats.PublicAccess.ReportPortNumber(result[1].EndPoint!.Port);
                }
                else
                {// Ipv6 not available, Ipv4 not available
                    this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Failure);
                    continue;
                }
            }

            this.netControl.NetStats.ReportEndpoint(true, result[0]);
            this.netControl.NetStats.ReportEndpoint(false, result[1]);
            this.nodeControl.ReportLifelineNode(netNode, ConnectionResult.Success);

            // this.logger.TryGet(LogLevel.Information)?.Log($"{netNode.Address.ToString()} - {r.Result.ToString()}");

            // Integrate online nodes.
            // using (var connection = await this.netControl.NetTerminal.Connect(netNode))
            // {
            //    if (connection is not null)
            //    {
            //        var service = connection.GetService<INodeControlService>();
            //        var r2 = await this.nodeControl.IntegrateOnlineNode(async (x, y) => await service.DifferentiateOnlineNode(x), default);
            //    }
            // }
        }

        return StateResult.Terminate;
    }

    [StateMethod(1)]
    protected async Task<StateResult> CheckStatus(StateParameter parameter)
    {// KeepOnlineNode
        if (this.nodeControl.CountOnline == 0)
        {// No online node
            this.logger.TryGet(LogLevel.Fatal)?.Log("No online nodes. Please check your network connection and add nodes to node_list.");
        }
        else
        {
            this.ShowStatus();
        }

        this.ChangeState(State.KeepOnlineNode);
        return StateResult.Continue;
    }

    [StateMethod(2)]
    protected async Task<StateResult> KeepOnlineNode(StateParameter parameter)
    {
        // Online -> Lifeline
        // Lifeline offline -> Remove

        return StateResult.Continue;
    }

    private void ShowStatus()
    {
        this.logger.TryGet()?.Log($"Lifeline online/offline: {this.nodeControl.CountLinfelineOnline}/{this.nodeControl.CountLinfelineOffline}, Online: {this.nodeControl.CountOnline}");
    }

    private async Task<NetEndpoint> PingNetNode(NetNode netNode, bool ipv6)
    {
        var endpointResolution = ipv6 ? EndpointResolution.Ipv6 : EndpointResolution.Ipv4;
        var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new(), 0, this.CancellationToken, endpointResolution);

        if (r.Result == NetResult.Success && r.Value is { } value)
        {// Success
            return value.Endpoint;
        }
        else
        {
            return default;
        }
    }
}
