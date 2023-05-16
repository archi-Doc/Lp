// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1300 // Element should begin with upper-case letter

[TinyhandObject]
public partial class PacketPing : IPacket
{
    public const int TextMaxLength = 32;

    public PacketId PacketId => PacketId.Ping;

    public bool AllowUnencrypted => true;

    public PacketPing()
    {
    }

    public PacketPing(string text)
    {
        this.Text = text;
    }

    [Key(0, AddProperty = "Text", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(TextMaxLength)]
    private string _text = string.Empty;

    public override string ToString() => $"{this.Text}";
}

[TinyhandObject]
public partial class PacketPingResponse : IPacket
{
    public PacketId PacketId => PacketId.PingResponse;

    public bool AllowUnencrypted => true;

    public PacketPingResponse()
    {
    }

    public PacketPingResponse(NodeAddress? nodeAddress, string text)
    {
        this.NodeAddress = nodeAddress;
        this.Text = text;
    }

    [Key(0, AddProperty = "Text", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(PacketPing.TextMaxLength)]
    private string _text = string.Empty;

    [Key(1)]
    public NodeAddress? NodeAddress { get; set; }

    public override string ToString() => $"{this.Text} - {this.NodeAddress}";
}
