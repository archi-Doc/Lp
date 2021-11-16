// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketPing : IPacket
{
    public PacketPing()
    {
    }

    public PacketPing(NodeInformation nodeInformation, string text)
    {
        this.NodeInformation = nodeInformation;
        this.Text = text;
    }

    public bool IsResponse => false;

    public PacketId Id => PacketId.Ping;

    [Key(0)]
    public NodeInformation NodeInformation { get; set; } = default!;

    [Key(1)]
    public string Text { get; set; } = default!;
}
