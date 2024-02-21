// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NetStats : ITinyhandSerializationCallback
{
    private static readonly long ResetMics = Mics.FromMinutes(5);

    public NetStats(ILogger<NetStats> logger, NetBase netBase, EssentialAddress essentialAddress)
    {
        this.logger = logger;
        this.netBase = netBase;
        this.EssentialAddress = essentialAddress;
    }

    #region FieldAndProperty

    private readonly object syncObject = new();
    private readonly ILogger logger;
    private readonly NetBase netBase;

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public EssentialAddress EssentialAddress { get; private set; }

    [Key(3)]
    public MyAddress MyIpv4Address { get; private set; } = new();

    [Key(4)]
    public MyAddress MyIpv6Address { get; private set; } = new();

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCreateEndPoint(in NetAddress address, out NetEndPoint endPoint)
    {
        endPoint = default;
        if (this.MyIpv6Address.AddressState == MyAddress.State.Fixed ||
            this.MyIpv6Address.AddressState == MyAddress.State.Changed)
        {// Ipv6 supported
            address.TryCreateIpv6(ref endPoint);
            if (endPoint.IsValid)
            {
                return true;
            }

            return address.TryCreateIpv4(ref endPoint);
        }
        else
        {// Ipv4
            return address.TryCreateIpv4(ref endPoint);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCreateEndPoint(NetNode node, out NetEndPoint endPoint)
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

        this.logger.TryGet()?.Log(result.ToString());
    }

    public string Dump()
    {
        return $"NetStats: EssentialAddress: {this.EssentialAddress.Dump()}, Ipv4 Address: {this.MyIpv4Address.Dump()}, Ipv6 Address: {this.MyIpv6Address.Dump()}, ";
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        this.LastMics = Mics.GetUtcNow();
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
        var utcNow = Mics.GetUtcNow();
        var range = new MicsRange(utcNow - Mics.FromMinutes(1), utcNow);
        if (!range.IsIn(this.LastMics))
        {
            this.Reset();
        }
    }
}
