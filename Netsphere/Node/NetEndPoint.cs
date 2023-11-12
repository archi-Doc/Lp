// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;

namespace Netsphere;

[TinyhandObject]
public readonly partial record struct NetEndPoint
{
    public NetEndPoint(IPEndPoint endPoint, ushort engagement)
    {
        this.EndPoint = endPoint;
        this.Engagement = engagement;
    }

    [Key(0)]
    public readonly IPEndPoint EndPoint;

    [Key(1)]
    public readonly ushort Engagement;

    public bool IsValid => this.EndPoint is not null;
}
