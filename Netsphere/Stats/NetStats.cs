// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "lockObject")]
public sealed partial class NetStats
{
    public const string Filename = "NetStat.tinyhand";

    private const int EndpointTrustCapacity = 32;
    private const int EndpointTrustMinimum = 1;

    public NetStats(ILogger<NetStats> logger, NetBase netBase, NodeControl nodeControl)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.NodeControl = nodeControl;
    }

    #region FieldAndProperty

    public NodeType NodeType
    {
        get
        {
            if (this.OutboundPort.TryGet(out var port))
            {
                return (port == this.netBase.NetOptions.Port && this.netBase.IsPortNumberSpecified) ?
                    NodeType.Direct :
                    NodeType.Cone;
            }
            else if (this.OutboundPort.UnableToFix)
            {
                return NodeType.Symmetric;
            }

            return NodeType.Unknown;
        }
    }

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public NodeControl NodeControl { get; private set; }

    [Key(2)]
    public TrustSource<IPEndPoint?> Ipv4Endpoint { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    [Key(3)]
    public TrustSource<IPEndPoint?> Ipv6Endpoint { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    [Key(4)]
    public TrustSource<int> OutboundPort { get; private set; } = new(EndpointTrustCapacity, EndpointTrustMinimum);

    [Key(5)]
    public int LastPort { get; private set; }

    [IgnoreMember]
    public NetNode? FixedNetNode { get; private set; }

    [IgnoreMember]
    public NodeType FixedNodeType { get; private set; }

    private readonly Lock lockObject = new();
    private readonly ILogger logger;
    private readonly NetBase netBase;

    #endregion

    public bool TryCreateEndpoint(ref NetAddress address, EndpointResolution endpointResolution, out NetEndpoint endPoint)
    {
        endPoint = default;
        if (endpointResolution == EndpointResolution.PreferIpv6)
        {
            if (this.Ipv6Endpoint.FixedOrDefault is not null)
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
        if (this.Ipv6Endpoint.FixedOrDefault is not null)
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
        var address = new NetAddress(this.Ipv4Endpoint.FixedOrDefault?.Address, this.Ipv6Endpoint.FixedOrDefault?.Address, (ushort)this.netBase.NetOptions.Port);
        return new(address, this.netBase.NodePublicKey);
    }

    public bool TryGetOwnNetNode([MaybeNullWhen(false)] out NetNode netNode)
    {
        var validIpv4 = this.Ipv4Endpoint.TryGet(out var ipv4);
        var validIpv6 = this.Ipv6Endpoint.TryGet(out var ipv6);
        if (validIpv4 || validIpv6)
        {
            netNode = new(new NetAddress(ipv4?.Address, ipv6?.Address, (ushort)this.netBase.NetOptions.Port), this.netBase.NodePublicKey);
            return true;
        }

        netNode = default;
        return false;
    }

    public void Reset()
    {
        this.Ipv4Endpoint.Clear();
        this.Ipv6Endpoint.Clear();
        this.OutboundPort.Clear();
    }

    public void Update()
    {
        if (this.FixedNetNode is null)
        {
            if (this.TryGetOwnNetNode(out var netNode))
            {
                this.FixedNetNode = netNode;
            }
        }

        if (this.FixedNodeType == NodeType.Unknown)
        {
            this.FixedNodeType = this.NodeType;
        }
    }

    public void ReportEndpoint(bool isIpv6, IPEndPoint? endpoint)
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

    [TinyhandOnSerializing]
    private void OnSerializing()
    {
        this.LastMics = Mics.GetUtcNow();
        this.LastPort = this.netBase.NetOptions.Port;
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized()
    {
        if (this.LastPort != this.netBase.NetOptions.Port)
        {
            this.Reset();
            return;
        }

        var utcNow = Mics.GetUtcNow();
        var range = new MicsRange(utcNow - Mics.FromMinutes(1), utcNow);
        if (!range.IsWithin(this.LastMics))
        {
            this.Reset();
        }
    }
}
