// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public partial class PacketGetNodeInformation : IPacket
{
    public PacketGetNodeInformation()
    {
    }

    public PacketId PacketId => PacketId.GetNodeInformation;

    public bool AllowUnencrypted => true;
}

[TinyhandObject]
public partial class PacketGetNodeInformationResponse : IPacket
{
    public PacketGetNodeInformationResponse()
    {
    }

    public PacketGetNodeInformationResponse(NodeInformation node)
    {
        this.Node = node;
    }

    public PacketId PacketId => PacketId.GetNodeInformationResponse;

    public bool AllowUnencrypted => true;

    [Key(0)]
    public NodeInformation Node { get; set; } = default!;
}
