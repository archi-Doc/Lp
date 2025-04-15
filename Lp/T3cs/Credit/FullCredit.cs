// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record FullCredit
{
    public FullCredit()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0)]
    public Credit Credit { get; protected set; } = new();

    [Key(1)]
    public CreditInformation CreditInformation { get; protected set; } = T3cs.CreditInformation.Default;

    [Key(2)]
    public StorageData<Borrower.GoshujinClass> Borrowers { get; protected set; } = new();
}
