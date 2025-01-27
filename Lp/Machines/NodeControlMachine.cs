﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Lp.Logging;
using Netsphere.Packet;
using Netsphere.Stats;

namespace Lp.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NodeControlMachine : Machine
{
    private const int ConsumeLifelineCount = 10;
    private const int FixEndpointCount = 10;

    public NodeControlMachine(ILogger<NodeControlMachine> logger, NetBase netBase, NetControl netControl, NodeControl nodeControl)
        : base()
    {
        this.logger = logger;
        this.modestLogger = new(logger);
        this.netBase = netBase;
        this.netControl = netControl;
        this.netStats = this.netControl.NetStats;
        this.nodeControl = nodeControl;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    private readonly ILogger logger;
    private readonly ModestLogger modestLogger;
    private readonly NetControl netControl;
    private readonly NetBase netBase;
    private readonly NetStats netStats;
    private readonly NodeControl nodeControl;
    private int count = 0;

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

            if (!this.nodeControl.TryGetUncheckedLifelineNode(out var netNode))
            {// No lifeline node
                break;
            }

            if (!netNode.Address.IsValidIpv4AndIpv6)
            {
                this.nodeControl.ReportLifelineNodeConnection(netNode, ConnectionResult.Failure);
                continue;
            }

            tasks.Add(this.PingIpv4AndIpv6(netNode, true));
        }

        try
        {
            var results = await Task.WhenAll(tasks).WaitAsync(this.CancellationToken);
        }
        catch
        {
            return StateResult.Terminate;
        }

        this.ChangeState(State.MaintainNode, true);
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> MaintainNode(StateParameter parameter)
    {
        if (!this.netControl.NetTerminal.IsActive)
        {// Not active
            return StateResult.Continue;
        }

        // Active -> Lifeline, Lifeline offline -> Remove
        this.nodeControl.MaintainLifelineNode(this.netStats.OwnNetNode);

        // Check lifeline node
        if (this.nodeControl.TryGetUncheckedLifelineNode(out var node))
        {
            _ = await this.PingIpv4AndIpv6(node, true);
        }

        // Lifeline Online -> Active
        // this.nodeControl.FromLifelineNodeToActiveNode();

        var max = Math.Max(this.nodeControl.CountActive, FixEndpointCount);
        do
        {
            // Integrate active nodes.
            if (this.nodeControl.TryGetActiveNode(out node))
            {
                await this.PingAndIntegrateActiveNode(node);
            }
            else
            {
                await this.ProcessRestorationNode();
                break;
            }

            if (this.netStats.OutboundPort.IsAboveMinimum &&
                this.netStats.Ipv4Endpoint.IsFixed &&
                this.netStats.Ipv6Endpoint.IsFixed)
            {// Fixed
                break;
            }
        }
        while (!this.CancellationToken.IsCancellationRequested &&
        this.count++ < FixEndpointCount);

        this.nodeControl.Trim(false, true);

        if (this.nodeControl.NoOnlineNode)
        {
            this.modestLogger.Interval(TimeSpan.FromMinutes(5), Hashed.Error.NoOnlineNode, LogLevel.Fatal)?.Log(Hashed.Error.NoOnlineNode);
        }

        this.TimeUntilRun = TimeSpan.FromSeconds(10);
        return StateResult.Continue;
    }

    [CommandMethod(WithLock = false)]
    protected CommandResult ShowStatus(bool showNodes = false)
    {
        this.logger.TryGet()?.Log($"{this.netStats.GetOwnNodeType().ToString()}: {this.netStats.GetOwnNetNode().ToString()}");
        this.logger.TryGet()?.Log($"Lifeline Online/Offline: {this.nodeControl.CountLinfelineOnline}/{this.nodeControl.CountLinfelineOffline}, Active: {this.nodeControl.CountActive}");

        if (showNodes)
        {
            this.nodeControl.ShowNodes();
        }

        return CommandResult.Success;
    }

    private async Task<bool> PingIpv4AndIpv6(NetNode netNode, bool isLifelineNode)
    {
        if (netNode.Equals(this.netStats.OwnNetNode))
        {
            return true;
        }

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
                    this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Failure);
                }

                return false;
            }
        }

        this.netStats.ReportEndpoint(true, result[0]);
        this.netStats.ReportEndpoint(false, result[1]);

        if (netNode.Address.IsValidIpv4AndIpv6)
        {// LifelineNode and ActiveNode are limited to DualAddress.
            if (isLifelineNode)
            {
                this.nodeControl.ReportLifelineNodeConnection(netNode, ConnectionResult.Success);
                this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Success);
            }
            else
            {
                this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Success);
            }
        }

        return true;
    }

    private async Task<bool> PingIpv4OrIpv6(NetNode netNode)
    {
        if (netNode.Equals(this.netStats.OwnNetNode))
        {
            return true;
        }

        if (this.netStats.OwnNetNode?.Address.IsValidIpv6 == true)
        {// ipv6
            this.logger.TryGet()?.Log($"PingIpv6: {netNode.ToString()}");
            await this.PingNetNode(netNode, true);
        }

        
        var ipv6Task = 
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
                this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Failure);
                return false;
            }
        }

        this.netStats.ReportEndpoint(true, result[0]);
        this.netStats.ReportEndpoint(false, result[1]);

        if (netNode.Address.IsValidIpv4AndIpv6)
        {// LifelineNode and ActiveNode are limited to DualAddress.
            this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Success);
        }

        return true;
    }

    private async Task<IPEndPoint?> PingNetNode(NetNode netNode, bool ipv6)
    {
        var endpointResolution = ipv6 ? EndpointResolution.Ipv6 : EndpointResolution.Ipv4;
        var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new(), 0, this.CancellationToken, endpointResolution);

        if (r.Result == NetResult.Success && r.Value is { } value)
        {// Success
            return value.SourceEndpoint.EndPoint;
        }
        else
        {
            return default;
        }
    }

    private async Task PingAndIntegrateActiveNode(NetNode netNode)
    {
        if (netNode.Equals(this.netStats.OwnNetNode))
        {
            return;
        }

        _ = await this.PingIpv4AndIpv6(netNode, false);

        using (var connection = await this.netControl.NetTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Failure);
            }
            else
            {
                var service = connection.GetService<Lp.Net.IBasalService>();
                if (service is not null)
                {
                    var r2 = await this.nodeControl.IntegrateActiveNode(async (x, y) => await service.DifferentiateActiveNode(x), this.CancellationToken);
                }
            }
        }
    }

    private async Task ProcessRestorationNode()
    {
        if (this.nodeControl.RestorationNode is { } restorationNode)
        {
            this.nodeControl.RestorationNode = default;

            _ = await this.PingIpv4AndIpv6(restorationNode, false);

            using (var connection = await this.netControl.NetTerminal.Connect(restorationNode))
            {
                if (connection is not null)
                {
                    var service = connection.GetService<Lp.Net.IBasalService>();
                    if (service is not null)
                    {
                        var r2 = await this.nodeControl.IntegrateActiveNode(async (x, y) => await service.DifferentiateActiveNode(x), this.CancellationToken);
                    }
                }
            }
        }
    }
}
