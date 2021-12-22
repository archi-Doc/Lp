// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Netsphere;

[TinyhandObject]
internal partial class PacketGetNodeInformation : IPacket
{
    public PacketGetNodeInformation()
    {
    }

    public PacketId PacketId => PacketId.GetNodeInformation;

    public bool AllowUnencrypted => true;
}

[TinyhandObject]
internal partial class PacketGetNodeInformationResponse : IPacket
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
