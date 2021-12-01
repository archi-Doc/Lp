// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
public partial class PacketPing : IPacket
{
    public PacketId Id => PacketId.Ping;

    public bool AllowUnencrypted => true;

    public PacketPing()
    {
    }

    public PacketPing(string text)
    {
        this.Text = text;
    }

    [Key(0)]
    public string Text { get; set; } = default!;

    public override string ToString() => $"{this.Text}";
}

[TinyhandObject]
public partial class PacketPingResponse : IPacket
{
    public PacketId Id => PacketId.PingResponse;

    public bool AllowUnencrypted => true;

    public PacketPingResponse()
    {
    }

    public PacketPingResponse(NodeAddress? nodeAddress, string text)
    {
        this.NodeAddress = nodeAddress;
        this.Text = text;
    }


    [Key(0)]
    public string Text { get; set; } = default!;

    [Key(1)]
    public NodeAddress? NodeAddress { get; set; }

    public override string ToString() => $"{this.Text} - {this.NodeAddress}";
}
