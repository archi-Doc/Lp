// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Time;

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class NetStats : ITinyhandSerializationCallback
{
    private static readonly long ResetMics = Mics.FromMinutes(5);

    public NetStats(NetBase netBase, EssentialAddress essentialAddress)
    {
        this.netBase = netBase;
        this.EssentialAddress = essentialAddress;
    }

    #region FieldAndProperty

    private readonly object syncObject = new();
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

    public NetNode GetMyNetNode()
    {
        var address = new NetAddress(this.MyIpv4Address.Address, this.MyIpv6Address.Address, (ushort)this.netBase.NetsphereOptions.Port);
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
        // if (!range.IsIn(this.LastMics))
        {
            this.Reset();
        }
    }
}
