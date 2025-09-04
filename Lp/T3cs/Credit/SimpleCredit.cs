// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record SimpleCredit : ICredit
{
    public static SimpleCredit Create()
    {
        return new();
    }

    public SimpleCredit()
    {
    }

    [Key(0)]
    public ConstraintsAndCovenants ConstraintsAndCovenants { get; private set; } = new();

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(1)]
    public Credit Credit { get; protected set; } = Credit.UnsafeConstructor();

    [Key(2)]
    public CreditInformation CreditInformation { get; protected set; } = CreditInformation.UnsafeConstructor();

    [Key(3)]
    public StoragePoint<OwnerData.GoshujinClass> Owners { get; protected set; } = new();
}
