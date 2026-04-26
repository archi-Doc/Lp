// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
public partial class CreditPoint : StoragePoint<CreditBase>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(1)]
    public Credit Credit { get; private set; }

    public CreditPoint(Credit credit)
        : base()
    {
        this.Credit = credit;
    }

    public partial class GoshujinClass
    {
    }
}

[TinyhandUnion(0, typeof(EquityCredit))]
public abstract partial record class CreditBase
{
}
