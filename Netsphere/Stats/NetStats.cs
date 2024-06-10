// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NetStats : ITinyhandSerializationCallback
{
    private static readonly long ResetMics = Mics.FromMinutes(5);

    public NetStats(ILogger<NetStats> logger, NetBase netBase, NodeControl nodeControl)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.NodeControl = nodeControl;
        this.NodeControl.Prepare(this.netBase.NetOptions.NodeList);
    }

    #region FieldAndProperty

    private readonly object syncObject = new();
    private readonly ILogger logger;
    private readonly NetBase netBase;

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public NodeControl NodeControl { get; private set; }

    [Key(2)]
    public MyAddress MyIpv4Address { get; private set; } = new();

    [Key(3)]
    public MyAddress MyIpv6Address { get; private set; } = new();

    #endregion

    public bool TryCreateEndpoint(ref NetAddress address, EndpointResolution endpointResolution, out NetEndpoint endPoint)
    {
        endPoint = default;
        if (endpointResolution == EndpointResolution.PreferIpv6)
        {
            if (this.MyIpv6Address.AddressState == MyAddress.State.Fixed ||
            this.MyIpv6Address.AddressState == MyAddress.State.Changed)
            {// Ipv6 supported
                address.TryCreateIpv6(ref endPoint);
                if (endPoint.IsValid)
                {
                    return true;
                }
            }

            // Ipv4
            return address.TryCreateIpv4(ref endPoint);
        }
        else if (endpointResolution == EndpointResolution.NetAddress)
        {
            if (address.IsValidIpv6)
            {
                address.TryCreateIpv6(ref endPoint);
                if (endPoint.IsValid)
                {
                    return true;
                }
            }

            return address.TryCreateIpv4(ref endPoint);
        }
        else if (endpointResolution == EndpointResolution.Ipv4)
        {
            return address.TryCreateIpv4(ref endPoint);
        }
        else if (endpointResolution == EndpointResolution.Ipv6)
        {
            return address.TryCreateIpv6(ref endPoint);
        }
        else
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCreateEndpoint(NetNode node, out NetEndpoint endPoint)
    {
        endPoint = default;
        if (this.MyIpv6Address.AddressState == MyAddress.State.Fixed ||
            this.MyIpv6Address.AddressState == MyAddress.State.Changed)
        {// Ipv6 supported
            node.Address.TryCreateIpv6(ref endPoint);
            if (endPoint.IsValid)
            {
                return true;
            }

            return node.Address.TryCreateIpv4(ref endPoint);
        }
        else
        {// Ipv4
            return node.Address.TryCreateIpv4(ref endPoint);
        }
    }

    public NetNode GetMyNetNode()
    {
        var address = new NetAddress(this.MyIpv4Address.Address, this.MyIpv6Address.Address, (ushort)this.netBase.NetOptions.Port);
        return new(address, this.netBase.NodePublicKey);
    }

    public void UpdateStats()
    {
        if (this.MyIpv4Address.AddressState == MyAddress.State.Changed ||
            this.MyIpv6Address.AddressState == MyAddress.State.Changed)
        {// Reset
            this.MyIpv4Address.Reset();
            this.MyIpv6Address.Reset();
        }
    }

    public void Reset()
    {
        this.MyIpv4Address.Reset();
        this.MyIpv6Address.Reset();
    }

    public void ReportAddress(AddressQueryResult result)
    {
        var priority = result.Uri is not null;
        if (result.Ipv6)
        {// Ipv6
            this.MyIpv6Address.ReportAddress(priority, result.Address);
        }
        else
        {// Ipv4
            this.MyIpv4Address.ReportAddress(priority, result.Address);
        }

        // this.logger.TryGet()?.Log(result.ToString());
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        this.LastMics = Mics.GetUtcNow();
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        var utcNow = Mics.GetUtcNow();
        var range = new MicsRange(utcNow - Mics.FromMinutes(1), utcNow);
        if (!range.IsWithin(this.LastMics))
        {
            this.Reset();
        }
    }
}
