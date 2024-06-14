// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject]
public sealed partial class PingPacket : IPacket
{
    public const int MaxMessageLength = 32;

    public static PacketType PacketType => PacketType.Ping;

    public PingPacket()
    {
    }

    public PingPacket(string message)
    {
        this.Message = message;
    }

    [Key(0, AddProperty = "Message", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(MaxMessageLength)]
    private string _message = string.Empty;

    public override string ToString() => $"{this.Message}";
}

[TinyhandObject]
public sealed partial class PingPacketResponse : IPacket
{
    public static PacketType PacketType => PacketType.PingResponse;

    public PingPacketResponse()
    {
    }

    public PingPacketResponse(NetEndpoint endpoint, string message, int versionInt, int phase)
    {
        this.Endpoint = endpoint;
        this.Message = message;
        this.VersionInt = versionInt;
        this.Phase = phase;
    }

    [Key(0, AddProperty = "Message", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(PingPacket.MaxMessageLength)]
    private string _message = string.Empty;

    [Key(1)]
    public NetEndpoint Endpoint { get; set; }
    // public NetAddress Address { get; set; }

    [Key(2)]
    public int VersionInt { get; set; }

    [Key(3)]
    public int Phase { get; set; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(this.Message))
        {
            return $"{this.Endpoint} {this.VersionInt}";
        }
        else
        {
            return $"{this.Message} {this.Endpoint} {this.VersionInt}";
        }
    }
}
