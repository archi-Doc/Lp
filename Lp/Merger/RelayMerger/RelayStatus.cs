// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject(Structural = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RelayStatus
{
    public RelayStatus()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NetAddress Address { get; private set; }
}
