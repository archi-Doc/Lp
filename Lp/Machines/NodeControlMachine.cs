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

            if (this.netStats.OutboundPort.IsInconsistent)
            {// Symmetric (random port)
                this.ShowStatus();
                this.ChangeState(State.MaintainOnlineNode);
                return StateResult.Continue;
            }

            if (!this.nodeControl.TryGetActiveNode(out var node))
            { // No online node
                this.logger.TryGet(LogLevel.Fatal)?.Log("No online nodes. Please check your network connection and add nodes to NodeList.");
                this.ChangeState(State.NoOnlineNode);
                return StateResult.Continue;
            }

            var result = await this.PingIpv4AndIpv6(node, false);
        }

        return StateResult.Terminate;
    }

    [StateMethod(2)]
    protected async Task<StateResult> MaintainOnlineNode(StateParameter parameter)
    {
        // Online -> Lifeline, Lifeline offline -> Remove
        this.nodeControl.MaintainLifelineNode();

        // Check lifeline node
        if (this.nodeControl.TryGetUncheckedLifelineNode(out var netNode))
        {
            _ = await this.PingIpv4AndIpv6(netNode, true);
        }

        // Check unknown node
        if (this.nodeControl.TryGetUnknownNode(out netNode))
        {
            _ = await this.PingIpv4AndIpv6(netNode, false);
        }

        // Add active nodes from lifeline nodes.
        if (this.nodeControl.CountActive == 0)
        {
            this.nodeControl.FromLifelineNodeToActiveNode();
        }

        // Integrate active nodes.
        if (this.nodeControl.TryGetActiveNode(out netNode))
        {
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
                        var r2 = await this.nodeControl.IntegrateOnlineNode(async (x, y) => await service.DifferentiateActiveNode(x), this.CancellationToken);
                    }
                }
            }
        }

        this.TimeUntilRun = TimeSpan.FromSeconds(10);
        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> NoOnlineNode(StateParameter parameter)
    {
        // Check unknown node
        if (this.nodeControl.TryGetUnknownNode(out var netNode))
        {
            _ = await this.PingIpv4AndIpv6(netNode, false);
        }

        return StateResult.Continue;
    }

    [CommandMethod]
    protected CommandResult ShowStatus()
    {
        this.logger.TryGet()?.Log($"{this.netStats.GetOwnNodeType().ToString()}: {this.netStats.GetOwnNetNode().ToString()}");
        this.logger.TryGet()?.Log($"Lifeline online/offline: {this.nodeControl.CountLinfelineOnline}/{this.nodeControl.CountLinfelineOffline}, Online: {this.nodeControl.CountActive}, Unknown: {this.nodeControl.CountUnknown}");

        return CommandResult.Success;
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
                    this.nodeControl.ReportActiveNodeConnection(netNode, ConnectionResult.Failure);
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
            return value.Endpoint.EndPoint;
        }
        else
        {
            return default;
        }
    }
}
