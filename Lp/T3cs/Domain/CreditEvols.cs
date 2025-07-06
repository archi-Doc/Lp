// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class CreditEvols
{
    public static readonly long DefaultValidMics = Mics.FromDays(10);

    public CreditEvols(Credit credit)
    {
        this.Credit = credit;
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public Credit Credit { get; private set; }

    [Key(1)]
    [Link(Type = ChainType.Ordered)]
    public long IntegratedMics { get; private set; }

    [Key(2)]
    public EvolLinkage.GoshujinClass Evols { get; private set; } = new();
}
