// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketData : IPacket
{
    public bool IsResponse => false;

    public PacketId Id => PacketId.Data;

    [Key(0)]
    public bool Response { get; set; }

    [Key(1)]
    public ushort NumberOfPackets { get; set; }

    [Key(2)]
    public uint DataSize { get; set; }
}
