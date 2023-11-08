// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using Netsphere.NetStats;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NetStatsMachine : Machine
{
    private const int NodeThreshold = 8;

    public NetStatsMachine(ILogger<NetStatsMachine> logger, LPBase lpBase, NetControl netControl, ICrystal<StatsData> statsData)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.netControl = netControl;
        this.statsData = statsData.Data;

        // this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Unknown(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("State: Unknown");

        this.statsData.UpdateStats();

        if (this.statsData.Ipv4State == NodeType.Unknown)
        {
            if (this.statsData.EssentialAddress.CountIpv4 < NodeThreshold)
            {
            }
        }

        return StateResult.Terminate;
    }

    private readonly ILogger logger;
    private readonly LPBase lpBase;
    private readonly NetControl netControl;
    private readonly StatsData statsData;
}
