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

    [Key(0, AddProperty = "", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    protected Credit credit = Credit.Default;

    [Key(1, AddProperty = "CreditInformation", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    protected CreditInformation creditInformation = CreditInformation.Default;

    [Key(2, AddProperty = "Borrowers", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    protected StorageData<BorrowerData.GoshujinClass> borrowers = new();
}*/
