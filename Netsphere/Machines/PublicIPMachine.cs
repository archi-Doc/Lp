// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Machines;

[MachineObject(0xc701fc35, Group = typeof(SingleGroup<>))]
public partial class PublicIPMachine : Machine<Identifier>
{
    private const string Filename = "PublicIP.tinyhand";
    private const string IcanhazipUri = "http://ipv4.icanhazip.com"; // "http://icanhazip.com"
    private const string DynDnsUri = "http://checkip.dyndns.org";

    [TinyhandObject(ImplicitKeyAsName = true)]
    private partial class Data
    {
        public long Mics { get; set; }

        public IPAddress? IPAddress { get; set; }
    }

    public PublicIPMachine(ILogger<PublicIPMachine> logger, BigMachine<Identifier> bigMachine, LPBase lpBase, NetControl netControl)
        : base(bigMachine)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.netControl = netControl;

        // this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        this.data = await this.lpBase.TryLoadUtf8Async<Data>(Filename) ?? new();
        if (this.data.IPAddress is not null &&
            Mics.IsInPeriodToUtcNow(this.data.Mics, Mics.FromMinutes(5)))
        {
            var nodeAddress = new NodeAddress(this.data.IPAddress, (ushort)this.netControl.NetBase.NetsphereOptions.Port);
            this.netControl.NetStatus.ReportMyNodeAddress(nodeAddress);
            this.logger?.TryGet()?.Log($"{nodeAddress.ToString()} from file");
            return StateResult.Terminate;
        }

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

    private async Task ReportIpAddress(IPAddress ipAddress, string uri)
    {
        var nodeAddress = new NodeAddress(ipAddress, (ushort)this.netControl.NetBase.NetsphereOptions.Port);
        this.netControl.NetStatus.ReportMyNodeAddress(nodeAddress);
        this.logger?.TryGet()?.Log($"{nodeAddress.ToString()} from {uri}");

        this.data ??= new();
        this.data.Mics = Mics.GetUtcNow();
        this.data.IPAddress = ipAddress;

        await this.lpBase.SaveUtf8Async(Filename, this.data);
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

                await this.ReportIpAddress(ipAddress, IcanhazipUri);
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

                await this.ReportIpAddress(ipAddress, DynDnsUri);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private ILogger? logger;
    private Data? data;
    private NetControl netControl;
    private LPBase lpBase;
}
