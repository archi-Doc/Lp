// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents ipv4/ipv6 address information.<br/>
/// Determine if the address is valid based on whether the port number is greater than zero.
/// </summary>
[TinyhandObject]
public readonly partial struct DualAddress
{
    [Key(0)]
    public readonly ushort Engagement4;

    [Key(1)]
    public readonly ushort Engagement6;

    [Key(2)]
    public readonly ushort Port4;

    [Key(3)]
    public readonly ushort Port6;

    [Key(4)]
    public readonly uint Address4;

    [Key(5)]
    public readonly ulong Address6A;

    [Key(6)]
    public readonly ulong Address6B;
}
