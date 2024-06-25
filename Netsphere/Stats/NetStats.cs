// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NetStats : ITinyhandSerializationCallback
{
    public NetStats(ILogger<NetStats> logger, NetBase netBase, NodeControl nodeControl, PublicAccess publicAccess)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.NodeControl = nodeControl;
        this.PublicAccess = publicAccess;
    }

    #region FieldAndProperty

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public NodeControl NodeControl { get; private set; }

    [Key(2)]
    public PublicAddress PublicIpv4Address { get; private set; } = new();

    [Key(3)]
    public PublicAddress PublicIpv6Address { get; private set; } = new();

    [Key(4)]
    public PublicAccess PublicAccess { get; private set; }

    [IgnoreMember]
    public TrustSource<NetEndpoint> Ipv4Endpoint { get; private set; } = new();

    [IgnoreMember]
    public TrustSource<NetEndpoint> Ipv6Endpoint { get; private set; } = new();

    private readonly object syncObject = new();
    private readonly ILogger logger;
    private readonly NetBase netBase;

    #endregion

    public bool TryCreateEndpoint(ref NetAddress address, EndpointResolution endpointResolution, out NetEndpoint endPoint)
    {
        endPoint = default;
        if (endpointResolution == EndpointResolution.PreferIpv6)
        {
            if (this.PublicIpv6Address.AddressState == PublicAddress.State.Fixed ||
            this.PublicIpv6Address.AddressState == PublicAddress.State.Changed)
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
        if (this.PublicIpv6Address.AddressState == PublicAddress.State.Fixed ||
            this.PublicIpv6Address.AddressState == PublicAddress.State.Changed)
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

    public NetNode GetOwnNetNode()
    {
        var address = new NetAddress(this.PublicIpv4Address.Address, this.PublicIpv6Address.Address, (ushort)this.netBase.NetOptions.Port);
        return new(address, this.netBase.NodePublicKey);
    }

    public void UpdateStats()
    {
        if (this.PublicIpv4Address.AddressState == PublicAddress.State.Changed ||
            this.PublicIpv6Address.AddressState == PublicAddress.State.Changed)
        {// Reset
            this.PublicIpv4Address.Reset();
            this.PublicIpv6Address.Reset();
        }
    }

    public void Reset()
    {
        this.PublicIpv4Address.Reset();
        this.PublicIpv6Address.Reset();
    }

    public void ReportEndpoint(bool isIpv6, NetEndpoint endpoint)
    {
        if (isIpv6)
        {
            this.Ipv6Endpoint.Add(endpoint);
        }
        else
        {
            this.Ipv4Endpoint.Add(endpoint);
        }
    }

    public void ReportAddress(AddressQueryResult result)
    {
        var priority = result.Uri is not null;
        if (result.IsValidIpv6)
        {// Ipv6
            this.PublicIpv6Address.ReportAddress(priority, result.Address);
        }
        else
        {// Ipv4
            this.PublicIpv4Address.ReportAddress(priority, result.Address);
        }
    }

    public void ReportAddress(IPAddress address)
    {
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {// Ipv6
            this.PublicIpv6Address.ReportAddress(false, address);
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {// Ipv4
            this.PublicIpv4Address.ReportAddress(false, address);
        }
    }

    void ITinyhandSerializationCallback.OnAfterReconstruct()
    {
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
