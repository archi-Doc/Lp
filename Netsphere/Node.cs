// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

public enum NodeType : byte
{
    Development,
    Release,
}

[TinyhandObject]
public partial class NodeAddress : IEquatable<NodeAddress>
{
    public NodeAddress()
    {
    }

    [Key(0)]
    public NodeType Type { get; set; }

    [Key(1)]
    public byte Engagement { get; set; }

    [Key(2)]
    public ushort Port { get; set; }

    [Key(3)]
    public IPAddress Address { get; set; } = IPAddress.None;

    public bool Equals(NodeAddress? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Type == other.Type && this.Engagement == other.Engagement && this.Port == other.Port && this.Address.Equals(other.Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Type, this.Engagement, this.Port, this.Address);
    }
}

[TinyhandObject]
public partial class NodeInformation : NodeAddress
{
    [Link(Primary = true, Type = ChainType.QueueList, Name = "Queue")]
    public NodeInformation()
    {
    }

    [Key(4)]
    protected ulong identifier0;

    public bool Equals(NodeInformation? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Type == other.Type && this.Engagement == other.Engagement && this.Port == other.Port && this.Address.Equals(other.Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Type, this.Engagement, this.Port, this.Address);
    }
}
