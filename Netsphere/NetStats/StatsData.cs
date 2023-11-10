// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.NetStats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class StatsData : ITinyhandSerializationCallback
{
    public StatsData(EssentialAddress essentialAddress)
    {
        this.EssentialAddress = essentialAddress;
    }

    #region FieldAndProperty

    private readonly object syncObject = new();

    [Key(0)]
    public EssentialAddress EssentialAddress { get; private set; }

    [Key(1)]
    public NodeType Ipv4State { get; private set; }

    [Key(2)]
    public NodeType Ipv6State { get; private set; }

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
            this.MyIpv4Address.Clear();
            this.MyIpv6Address.Clear();
            this.Ipv4State = NodeType.Unknown;
            this.Ipv6State = NodeType.Unknown;
        }

        this.Ipv4State = NodeType.Unknown;
        this.Ipv6State = NodeType.Unknown; // tempcode
    }

    public void AddressFixed()
    {
        this.Ipv4State = NodeType.Global;
        this.Ipv6State = NodeType.Global;
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

    public MyAddress.State GetAddressState()
    {
        return MyAddress.State.Unknown;
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
    }
}
