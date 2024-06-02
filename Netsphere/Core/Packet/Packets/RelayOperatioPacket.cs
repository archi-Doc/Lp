// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Relay;

[TinyhandObject]
public sealed partial class RelayOperatioPacket : IPacket
{
    public static PacketType PacketType => PacketType.RelayOperation;

    public enum Operation
    {
        SetOuterEndPoint,
        Close,
    }

    public RelayOperatioPacket()
    {
    }

    public static RelayOperatioPacket SetOuterEndPoint(NetEndpoint outerEndPoint)
    {
        return new()
        {
            RelayOperation = Operation.SetOuterEndPoint,
            OuterEndPoint = outerEndPoint,
        };
    }

    public static RelayOperatioPacket CreateClose()
    {
        return new()
        {
            RelayOperation = Operation.Close,
        };
    }

    [Key(0)]
    public Operation RelayOperation { get; set; }

    [Key(1)]
    public NetEndpoint OuterEndPoint { get; set; }
}

[TinyhandObject]
public sealed partial class RelayOperatioResponse : IPacket
{
    public static PacketType PacketType => PacketType.RelayOperationResponse;

    public RelayOperatioResponse()
    {
    }

    public RelayOperatioResponse(RelayResult result)
    {
        this.Result = result;
    }

    [Key(0)]
    public RelayResult Result { get; private set; }
}
