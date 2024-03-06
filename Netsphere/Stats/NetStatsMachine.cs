// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Netsphere.Stats;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NetStatsMachine : Machine
{
    private const int NodeThreshold = 4;

    public NetStatsMachine(ILogger<NetStatsMachine> logger, NetControl netControl, NetStats statsData)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.netStats = statsData;

        this.DefaultTimeout = TimeSpan.FromSeconds(5);

        // var port = this.netControl.NetBase.NetsphereOptions.Port;
    }

    [StateMethod(0)]
    protected async Task<StateResult> Unknown(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("Unknown");

        this.netStats.UpdateStats();

        if (this.netStats.MyIpv4Address.AddressState != MyAddress.State.Unknown &&
            this.netStats.MyIpv6Address.AddressState != MyAddress.State.Unknown)
        {// Address has been fixed.
            if (this.netStats.MyIpv4Address.AddressState == MyAddress.State.Unavailable && this.netStats.MyIpv6Address.AddressState == MyAddress.State.Unavailable)
            {
                this.netStats.Reset();
            }
            else
            {
                this.ChangeState(State.AddressFixed, true);
                return StateResult.Continue;
            }
        }

        var tasks = new List<Task<AddressQueryResult>>();
        if (this.netStats.MyIpv4Address.AddressState == MyAddress.State.Unknown)
        {
            if (this.netStats.EssentialAddress.CountIpv4 < NodeThreshold)
            {
                tasks.Add(NetStatsHelper.GetIcanhazipIPv4(this.CancellationToken));
            }
            else
            {
            }
        }

        if (this.netStats.MyIpv6Address.AddressState == MyAddress.State.Unknown)
        {
            if (this.netStats.EssentialAddress.CountIpv6 < NodeThreshold)
            {
                tasks.Add(NetStatsHelper.GetIcanhazipIPv6(this.CancellationToken));
            }
            else
            {
            }
        }

        var results = await Task.WhenAll(tasks);
        foreach (var x in results)
        {
            this.netStats.ReportAddress(x);
        }

        if (this.netStats.MyIpv4Address.AddressState != MyAddress.State.Unknown &&
             this.netStats.MyIpv6Address.AddressState != MyAddress.State.Unknown)
        {// Address has been fixed.
            this.ChangeState(State.AddressFixed, true);
            return StateResult.Continue;
        }

        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> AddressFixed(StateParameter parameter)
    {
        this.logger.TryGet()?.Log(this.netStats.Dump());
        this.logger.TryGet()?.Log(this.netStats.GetMyNetNode().ToString());

        return StateResult.Terminate;
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly NetStats netStats;
}
