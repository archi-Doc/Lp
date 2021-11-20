// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Net;

[TinyhandObject]
public partial class RawPacketPing : IRawPacket
{
    public RawPacketPing()
    {
    }

    public RawPacketPing(string text)
    {
        this.Text = text;
    }

    public RawPacketId Id => RawPacketId.Ping;

    [Key(0)]
    public string Text { get; set; } = default!;

    public override string ToString() => $"{this.Text}";
}

[TinyhandObject]
public partial class RawPacketPingResponse : IRawPacket
{
    public RawPacketPingResponse()
    {
    }

    public RawPacketPingResponse(NodeAddress? nodeAddress, string text)
    {
        this.NodeAddress = nodeAddress;
        this.Text = text;
    }

    public RawPacketId Id => RawPacketId.PingResponse;

    [Key(0)]
    public string Text { get; set; } = default!;

    [Key(1)]
    public NodeAddress? NodeAddress { get; set; }

    public override string ToString() => $"{this.Text} - {this.NodeAddress}";
}
