// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;

namespace LP.T3CS;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record CreditData
{
    public CreditData()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0, AddProperty = "Credit", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private Credit credit = Credit.Default;

    [Key(1, AddProperty = "CreditInformation", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private CreditInformation creditInformation = CreditInformation.Default;
}
