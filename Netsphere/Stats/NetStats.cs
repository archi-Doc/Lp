// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NetStats : ITinyhandSerializationCallback
{
    private const int EndpointTrustCapacity = 32;
    private const int EndpointTrustMinimum = 4;
    private const int PortTrustCapacity = 32;
    private const int PortTrustMinimum = 4;

    public NetStats(ILogger<NetStats> logger, NetBase netBase, NodeControl nodeControl)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.NodeControl = nodeControl;
    }

    #region FieldAndProperty

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public NodeControl NodeControl { get; private set; }

    /*[Key(2)]
    public PublicAddress PublicIpv4Address { get; private set; } = new();

    [Key(3)]
    public PublicAddress PublicIpv6Address { get; private set; } = new();*/

    [IgnoreMember]
    public TrustSource<NetEndpoint> Ipv4Endpoint { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    [IgnoreMember]
    public TrustSource<NetEndpoint> Ipv6Endpoint { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    [IgnoreMember]
    public TrustSource<int> OutboundPort { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    private readonly object syncObject = new();
    private readonly ILogger logger;
    private readonly NetBase netBase;

    #endregion

    public bool TryCreateEndpoint(ref NetAddress address, EndpointResolution endpointResolution, out NetEndpoint endPoint)
    {
        endPoint = default;
        if (endpointResolution == EndpointResolution.PreferIpv6)
        {
            if (this.Ipv6Endpoint.IsFixed)
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
        if (this.Ipv6Endpoint.IsFixed)
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
        var address = new NetAddress(this.Ipv4Endpoint.FixedOrDefault.EndPoint?.Address, this.Ipv6Endpoint.FixedOrDefault.EndPoint?.Address, (ushort)this.netBase.NetOptions.Port);
        return new(address, this.netBase.NodePublicKey);
    }

    public void Reset()
    {
        // this.PublicIpv4Address.Reset();
        // this.PublicIpv6Address.Reset();
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
