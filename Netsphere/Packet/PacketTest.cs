// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
internal partial class PacketTest : IPacket
{
    public PacketTest()
    {
    }

    public PacketTest(NodeInformation nodeInformation, string text)
    {
        this.NodeInformation = nodeInformation;
        this.Text = text;
    }

    public bool IsResponse => false;

    public PacketId Id => PacketId.Data;

    [Key(0)]
    public NodeInformation NodeInformation { get; set; } = default!;

    [Key(1)]
    public string Text { get; set; } = default!;
}
