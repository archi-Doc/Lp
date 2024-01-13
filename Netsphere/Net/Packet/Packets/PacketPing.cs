// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class PacketPing : IPacket, IBlock
{
    public const int MaxMessageLength = 32;

    public static PacketType PacketType => PacketType.Ping;

    public uint BlockId => (uint)PacketType;

    public PacketPing()
    {
    }

    public PacketPing(string message)
    {
        this.Message = message;
    }

    [Key(0, AddProperty = "Message", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(MaxMessageLength)]
    private string _message = string.Empty;

    public override string ToString() => $"{this.Message}";
}

[TinyhandObject]
public sealed partial class PacketPingResponse : IPacket, IBlock
{
    public static PacketType PacketType => PacketType.PingResponse;

    public uint BlockId => (uint)PacketType;

    public PacketPingResponse()
    {
    }

    public PacketPingResponse(NetAddress address, string message)
    {
        this.Address = address;
        this.Message = message;
    }

    [Key(0, AddProperty = "Message", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(PacketPing.MaxMessageLength)]
    private string _message = string.Empty;

    [Key(1)]
    public NetAddress Address { get; set; }

    public override string ToString() => $"{this.Message} - {this.Address}";
}
