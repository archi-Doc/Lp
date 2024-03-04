// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Netsphere.Stats;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NetStatsMachine : Machine
{
    private const int NodeThreshold = 4;
    private const string IcanhazipUriIPv4 = "http://ipv4.icanhazip.com";
    private const string IcanhazipUriIPv6 = "http://ipv6.icanhazip.com";
    private const string DynDnsUri = "http://checkip.dyndns.org";
    private static readonly TimeSpan GetTimeout = TimeSpan.FromSeconds(2);

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
            if (this.netStats.MyIpv4Address.AddressState != MyAddress.State.Unavailable || this.netStats.MyIpv6Address.AddressState != MyAddress.State.Unavailable)
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
                tasks.Add(this.GetIcanhazipIPv4());
            }
            else
            {
            }
        }

        if (this.netStats.MyIpv6Address.AddressState == MyAddress.State.Unknown)
        {
            if (this.netStats.EssentialAddress.CountIpv6 < NodeThreshold)
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

    /*private void ReportIpAddress(IPAddress ipAddress, string uri)
    {
        var nodeAddress = new NodeAddress(ipAddress, (ushort)this.netControl.NetBase.NetsphereOptions.Port);
        this.netControl.NetStatus.ReportMyNodeAddress(nodeAddress);
        this.logger?.TryGet()?.Log($"{nodeAddress.ToString()} from {uri}");
    }*/

    private async Task<AddressQueryResult> GetIcanhazipIPv4()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv4, this.CancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);
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
                var result = await httpClient.GetStringAsync(IcanhazipUriIPv6, this.CancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);
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
                var result = await httpClient.GetStringAsync(DynDnsUri, this.CancellationToken).WaitAsync(GetTimeout).ConfigureAwait(false);

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
