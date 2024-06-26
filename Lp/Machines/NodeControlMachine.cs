// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Netsphere;
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
    private const int ConsumeLifelineCount = 10;

    public NodeControlMachine(ILogger<NodeControlMachine> logger, NetBase netBase, NetControl netControl, NodeControl nodeControl)
        : base()
    {
        this.logger = logger;
        this.netBase = netBase;
        this.netControl = netControl;
        this.netStats = this.netControl.NetStats;
        this.nodeControl = nodeControl;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly NetBase netBase;
    private readonly NetStats netStats;
    private readonly NodeControl nodeControl;

    [StateMethod(0)]
    protected async Task<StateResult> ConsumeLifelineNode(StateParameter parameter)
    {
        if (!this.netControl.NetTerminal.IsActive)
        {
            return StateResult.Continue;
        }

        var count = 0;
        var tasks = new List<Task<bool>>();
        while (true)
        {
            if (count++ >= ConsumeLifelineCount)
            {
                break;
            }

            if (!this.nodeControl.TryGetLifelineNode(out var netNode))
            {// No lifeline node
                break;
            }

            if (!netNode.Address.IsValidIpv4AndIpv6)
            {
                this.nodeControl.ReportLifelineNodeConnection(netNode, ConnectionResult.Failure);
                continue;
            }

            tasks.Add(this.PingIpv4AndIpv6(netNode, true));

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

        try
        {
            var results = await Task.WhenAll(tasks).WaitAsync(this.CancellationToken);
        }
        catch
        {
            return StateResult.Terminate;
        }

        this.ChangeState(State.FixEndpoint, true);
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> FixEndpoint(StateParameter parameter)
    {
        while (!this.CancellationToken.IsCancellationRequested)
        {
            if (this.netStats.Ipv4Endpoint.IsFixed && this.netStats.Ipv6Endpoint.IsFixed)
            {// Fixed
                this.ShowStatus();

                this.ChangeState(State.MaintainOnlineNode);
                return StateResult.Continue;
            }

            if (!this.nodeControl.TryGetOnlineNode(out var node))
            {
                // No online node
                this.logger.TryGet(LogLevel.Fatal)?.Log("No online nodes. Please check your network connection and add nodes to node_list.");
                this.ChangeState(State.NoOnlineNode);
                return StateResult.Continue;
            }

            var result = await this.PingIpv4AndIpv6(node, false);
            await Task.Delay(1000);
        }

        return StateResult.Terminate;
    }

    [StateMethod(2)]
    protected async Task<StateResult> MaintainOnlineNode(StateParameter parameter)
    {
        // Online -> Lifeline
        // Lifeline offline -> Remove

        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> NoOnlineNode(StateParameter parameter)
    {
        return StateResult.Continue;
    }

    private void ShowStatus()
    {
        this.logger.TryGet()?.Log($"Fixed: {this.netStats.GetOwnNetNode().ToString()}");
        this.logger.TryGet()?.Log($"Lifeline online/offline: {this.nodeControl.CountLinfelineOnline}/{this.nodeControl.CountLinfelineOffline}, Online: {this.nodeControl.CountOnline}");
    }

    private async Task<bool> PingIpv4AndIpv6(NetNode netNode, bool isLifelineNode)
    {
        this.logger.TryGet()?.Log($"PingIpv4AndIpv6: {netNode.ToString()}");
        var ipv6Task = this.PingNetNode(netNode, true);
        var ipv4Task = this.PingNetNode(netNode, false);
        var result = await Task.WhenAll(ipv6Task, ipv4Task);

        if (result[0] is not null)
        {
            if (result[1] is not null)
            {// Ipv6 available, Ipv4 available
                this.netStats.OutboundPort.Add(result[0]!.Port);
            }
            else
            {// Ipv6 available, Ipv4 not available
                this.netStats.OutboundPort.Add(result[0]!.Port);
            }
        }
        else
        {
            if (result[1] is not null)
            {// Ipv6 not available, Ipv4 available
                this.netStats.OutboundPort.Add(result[1]!.Port);
            }
            else
            {// Ipv6 not available, Ipv4 not available
                if (isLifelineNode)
                {
                    this.nodeControl.ReportLifelineNodeConnection(netNode, ConnectionResult.Failure);
                }
                else
                {
                    this.nodeControl.ReportOnlineNodeConnection(netNode, ConnectionResult.Failure);
                }

                return false;
            }
        }

        this.netStats.ReportEndpoint(true, result[0]);
        this.netStats.ReportEndpoint(false, result[1]);

        if (isLifelineNode)
        {
            this.nodeControl.ReportLifelineNodeConnection(netNode, ConnectionResult.Success);
        }
        else
        {
            this.nodeControl.ReportOnlineNodeConnection(netNode, ConnectionResult.Success);
        }

        return true;
    }

    private async Task<IPEndPoint?> PingNetNode(NetNode netNode, bool ipv6)
    {
        var endpointResolution = ipv6 ? EndpointResolution.Ipv6 : EndpointResolution.Ipv4;
        var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new(), 0, this.CancellationToken, endpointResolution);

        if (r.Result == NetResult.Success && r.Value is { } value)
        {// Success
            return value.Endpoint.EndPoint;
        }
        else
        {
            return default;
        }
    }
}
