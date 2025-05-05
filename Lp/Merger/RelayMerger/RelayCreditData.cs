// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

#pragma warning disable SA1401

/*[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RelayCreditData : CreditDataBase
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "credit")]
    public RelayCreditData()
    {
    }
}

public partial record CreditDataBase
{
    public CreditDataBase()
    {
    }

    [Key(0)]
    public Credit Credit { get; init; } = Credit.Default;

    [Key(1)]
    protected CreditInformation CreditInformation { get; init; } = CreditInformation.Default;

    [Key(2)]
    protected StorageData<BorrowerData.GoshujinClass> Borrowers { get; init; } = new();
}*/
