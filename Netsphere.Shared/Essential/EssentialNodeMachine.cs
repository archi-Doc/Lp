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
public partial class EssentialNodeMachine : Machine
{
    public EssentialNodeMachine(ILogger<EssentialNodeMachine> logger, NetBase netBase, NetControl netControl, NetStats netStats)
        : base()
    {
        this.logger = logger;
        this.netBase = netBase;
        this.netControl = netControl;
        this.netStats = netStats;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly NetBase netBase;
    private readonly NetStats netStats;

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {//
        if (!this.netStats.EssentialNode.GetUncheckedNode(out var netNode))
        {
            return StateResult.Terminate;
        }

        // var node = await this.netControl.NetTerminal.UnsafeGetNetNode(netAddress);
        var r = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(netNode.Address, new());

        this.logger.TryGet(LogLevel.Information)?.Log($"{netNode.Address.ToString()} - {r.Result.ToString()}");
        if (r.Result == NetResult.Success && r.Value is { } value)
        {// Success
            this.netStats.EssentialNode.Report(netNode, ConnectionResult.Success);
        }
        else
        {
            this.netStats.EssentialNode.Report(netNode, ConnectionResult.Failure);
        }

        using (var connection = await this.netControl.NetTerminal.Connect(netNode))
        {
            if (connection is not null)
            {
                var service = connection.GetService<IEssentialService>();
                await this.netStats.EssentialNode.Integrate(async (x, y) => await service.IntegrateEssentialNode(x));
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
