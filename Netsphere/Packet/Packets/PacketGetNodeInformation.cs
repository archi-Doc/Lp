// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public partial class PacketGetNodeInformation : IPacket
{
    public PacketGetNodeInformation()
    {
    }

    public PacketIdObsolete PacketId => PacketIdObsolete.GetNodeInformation;

    public bool AllowUnencrypted => true;
}

[TinyhandObject]
public partial class PacketGetNodeInformationResponse : IPacket
{
    public PacketGetNodeInformationResponse()
    {
    }

    public PacketGetNodeInformationResponse(NetNode node)
    {
        this.Node = node;
    }

    public PacketIdObsolete PacketId => PacketIdObsolete.GetNodeInformationResponse;

    public bool AllowUnencrypted => true;

    [Key(0)]
    public NetNode Node { get; set; } = default!;
}
