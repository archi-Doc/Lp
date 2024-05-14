﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

#pragma warning disable SA1401

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RelayStatus
{
    public RelayStatus()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0, AddProperty = "Credit", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private Credit credit = Credit.Default;

    [Key(1, AddProperty = "Address", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private NetAddress address;
}
