// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1300 // Element should begin with upper-case letter

[TinyhandObject]
public partial class PacketPingObsolete : IPacketObsolete
{
    public const int TextMaxLength = 32;

    public PacketIdObsolete PacketId => PacketIdObsolete.Ping;

    public bool AllowUnencrypted => true;

    public PacketPingObsolete()
    {
    }

    public PacketPingObsolete(string text)
    {
        this.Text = text;
    }

    [Key(0, AddProperty = "Text", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(TextMaxLength)]
    private string _text = string.Empty;

    public override string ToString() => $"{this.Text}";
}

[TinyhandObject]
public partial class PacketPingResponseObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.PingResponse;

    public bool AllowUnencrypted => true;

    public PacketPingResponseObsolete()
    {
    }

    public PacketPingResponseObsolete(NetAddress address, string text)
    {
        this.Address = address;
        this.Text = text;
    }

    [Key(0, AddProperty = "Text", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(PacketPingObsolete.TextMaxLength)]
    private string _text = string.Empty;

    [Key(1)]
    public NetAddress Address { get; set; }

    public override string ToString() => $"{this.Text} - {this.Address}";
}
