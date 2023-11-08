// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.NetStats;

[TinyhandObject(UseServiceProvider = true, LockObject = "syncObject")]
public sealed partial class StatsData
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

    #endregion

    public void UpdateStats()
    {
    }
}
