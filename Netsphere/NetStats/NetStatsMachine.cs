// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using CrystalData;
using Netsphere.NetStats;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NetStatsMachine : Machine
{
    private const int NodeThreshold = 4;
    private const string IcanhazipUriIPv4 = "http://ipv4.icanhazip.com";
    private const string IcanhazipUriIPv6 = "http://ipv6.icanhazip.com";
    private const string DynDnsUri = "http://checkip.dyndns.org";

    public NetStatsMachine(ILogger<NetStatsMachine> logger, LPBase lpBase, NetControl netControl, ICrystal<StatsData> statsData)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.netControl = netControl;
        this.statsData = statsData.Data;

        this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Unknown(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("Unknown");

        this.statsData.UpdateStats();

        if (this.statsData.Ipv4State != NodeType.Unknown &&
            this.statsData.Ipv6State != NodeType.Unknown)
        {// Address has been fixed.
            this.ChangeState(State.AddressFixed, true);
            return StateResult.Continue;
        }

        var tasks = new List<Task<AddressQueryResult>>();
        if (this.statsData.Ipv4State == NodeType.Unknown)
        {
            if (this.statsData.EssentialAddress.CountIpv4 < NodeThreshold)
            {
                tasks.Add(this.GetIcanhazipIPv4());
            }
            else
            {
            }
        }

        if (this.statsData.Ipv6State == NodeType.Unknown)
        {
            if (this.statsData.EssentialAddress.CountIpv6 < NodeThreshold)
            {
                tasks.Add(this.GetIcanhazipIPv6());
            }
            else
            {
            }
        }

        var results = await Task.WhenAll(tasks);
        foreach (var x in results)
        {
            this.statsData.ReportAddress(x);
        }

        if (this.statsData.Ipv4State != NodeType.Unknown &&
            this.statsData.Ipv6State != NodeType.Unknown)
        {// Address has been fixed.
            this.ChangeState(State.AddressFixed, true);
            return StateResult.Continue;
        }

        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> AddressFixed(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("AddressFixed");

        return StateResult.Terminate;
    }

    private readonly ILogger logger;
    private readonly LPBase lpBase;
    private readonly NetControl netControl;
    private readonly StatsData statsData;

    private void ReportIpAddress(IPAddress ipAddress, string uri)
    {
        var nodeAddress = new NodeAddress(ipAddress, (ushort)this.netControl.NetBase.NetsphereOptions.Port);
        this.netControl.NetStatus.ReportMyNodeAddress(nodeAddress);
        this.logger?.TryGet()?.Log($"{nodeAddress.ToString()} from {uri}");
    }

    private async Task<AddressQueryResult> GetIcanhazipIPv4()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv4, this.CancellationToken).ConfigureAwait(false);
                var ipString = result.Replace("\\r\\n", string.Empty).Replace("\\n", string.Empty).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(false, IcanhazipUriIPv4, ipAddress);
            }
        }
        catch
        {
            return new(false, IcanhazipUriIPv4, default);
        }
    }

    private async Task<AddressQueryResult> GetIcanhazipIPv6()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv6, this.CancellationToken).ConfigureAwait(false);
                var ipString = result.Replace("\\r\\n", string.Empty).Replace("\\n", string.Empty).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(true, IcanhazipUriIPv6, ipAddress);
            }
        }
        catch
        {
            return new(true, IcanhazipUriIPv6, default);
        }
    }

    private async Task<AddressQueryResult> GetDynDns()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(DynDnsUri, this.CancellationToken).ConfigureAwait(false);

                var start = result.IndexOf(':');
                if (start < 0)
                {
                    return default;
                }

                var end = result.IndexOf('<', start + 1);
                if (end < 0)
                {
                    return default;
                }

                var ipString = result.Substring(start + 1, end - start - 1).Trim();
                IPAddress.TryParse(ipString, out var ipAddress);
                return new(false, DynDnsUri, ipAddress);
            }
        }
        catch
        {
            return default;
        }
    }
}
