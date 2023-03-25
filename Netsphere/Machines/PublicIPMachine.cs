// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;

namespace Netsphere.Machines;

[MachineObject(0xc701fc35, Group = typeof(SingleGroup<>))]
public partial class PublicIPMachine : Machine<Identifier>
{
    private const string IcanhazipUri = "http://ipv4.icanhazip.com"; // "http://icanhazip.com"
    private const string DynDnsUri = "http://checkip.dyndns.org";

    public PublicIPMachine(ILogger<PublicIPMachine> logger, BigMachine<Identifier> bigMachine, LPBase lpBase, NetBase netBase, NetControl netControl)
        : base(bigMachine)
    {
        this.logger = logger;
        this.NetControl = netControl;

        // this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    public NetControl NetControl { get; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (await this.GetIcanhazip().ConfigureAwait(false) == true)
        {
            return StateResult.Terminate;
        }
        else if (await this.GetDynDns().ConfigureAwait(false) == true)
        {
            return StateResult.Terminate;
        }

        return StateResult.Terminate;
    }

    private void ReportIpAddress(IPAddress ipAddress, string uri)
    {
        var nodeAddress = new NodeAddress(ipAddress, (ushort)this.NetControl.NetBase.NetsphereOptions.Port);
        this.NetControl.NetStatus.ReportMyNodeAddress(nodeAddress);
        this.logger?.TryGet()?.Log($"{nodeAddress.ToString()} from {uri}");
    }

    private async Task<bool> GetIcanhazip()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(IcanhazipUri, this.CancellationToken).ConfigureAwait(false);
                var ipString = result.Replace("\\r\\n", string.Empty).Replace("\\n", string.Empty).Trim();
                if (!IPAddress.TryParse(ipString, out var ipAddress))
                {
                    return false;
                }

                this.ReportIpAddress(ipAddress, IcanhazipUri);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> GetDynDns()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetStringAsync(DynDnsUri, this.CancellationToken).ConfigureAwait(false);

                var start = result.IndexOf(':');
                if (start < 0)
                {
                    return false;
                }

                var end = result.IndexOf('<', start + 1);
                if (end < 0)
                {
                    return false;
                }

                var ipString = result.Substring(start + 1, end - start - 1).Trim();
                if (!IPAddress.TryParse(ipString, out var ipAddress))
                {
                    return false;
                }

                this.ReportIpAddress(ipAddress, DynDnsUri);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private ILogger? logger;
}
