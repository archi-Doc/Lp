// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public readonly partial record struct NetEndpoint : IEquatable<NetEndpoint>
{
    public NetEndpoint(IPEndPoint endPoint, ushort relayId)
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

    public bool Equals(NetEndpoint endPoint)
        => this.RelayId == endPoint.RelayId &&
        this.EndPoint.Equals(endPoint.EndPoint);

    public override int GetHashCode()
        => HashCode.Combine(this.RelayId, this.EndPoint);

    public override string ToString()
        => this.EndPoint.ToString();
}
