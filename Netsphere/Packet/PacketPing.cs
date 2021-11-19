// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
public partial class PacketPing : IUnmanagedPacket
{
    public PacketPing()
    {
    }

    public PacketPing(NodeAddress? nodeAddress, string text)
    {
        this.NodeAddress = nodeAddress;
        this.Text = text;
    }

    public UnmanagedPacketId Id => UnmanagedPacketId.Ping;

    [Key(0)]
    public NodeAddress? NodeAddress { get; set; }

    [Key(1)]
    public string Text { get; set; } = default!;

    public override string ToString() => $"{this.NodeAddress} - {this.Text}";
}
