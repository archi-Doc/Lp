// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.NetStats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class StatsData : ITinyhandSerializationCallback
{
    private static readonly long ResetMics = Mics.FromMinutes(5);

    public StatsData(EssentialAddress essentialAddress)
    {
        this.EssentialAddress = essentialAddress;
    }

    #region FieldAndProperty

    private readonly object syncObject = new();

    [Key(0)]
    public long LastMics { get; private set; }

    [Key(1)]
    public EssentialAddress EssentialAddress { get; private set; }

    [Key(3)]
    public MyAddress MyIpv4Address { get; private set; } = default!;

    [Key(4)]
    public MyAddress MyIpv6Address { get; private set; } = default!;

    #endregion

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
        if (!range.IsIn(this.LastMics))
        {
            this.Reset();
        }
    }
}
