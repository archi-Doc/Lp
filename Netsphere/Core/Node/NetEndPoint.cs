// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public readonly partial record struct NetEndPoint
{
    public NetEndPoint(IPEndPoint endPoint, ushort relayId)
    {
        this.EndPoint = endPoint;
        this.RelayId = relayId;
    }

    [Key(0)]
    public readonly IPEndPoint EndPoint;

    [Key(1)]
    public readonly ushort RelayId;

    public bool IsValid
        => this.EndPoint is not null;

    /*public NetAddress ToNetAddress()
        => new NetAddress(this.EndPoint.Address, (ushort)this.EndPoint.Port);*/

    public bool IsPrivateOrLocalLoopbackAddress()
        => new NetAddress(this.EndPoint.Address, (ushort)this.EndPoint.Port).IsPrivateOrLocalLoopbackAddress();

    public bool EndPointEquals(IPEndPoint endPoint)
        => this.EndPoint.Equals(endPoint);

    public override string ToString()
        => this.EndPoint.ToString();
}
