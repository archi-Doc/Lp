// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Net;
using Arc.Threading;

namespace LP.Net;

public enum NodeType : byte
{
    Development,
    Release,
}

[TinyhandObject]
public partial class NodeAddress : IEquatable<NodeAddress>
{
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
        return HashCode.Combine(this.Type, this, Engagement, this.Port, this.Address);
    }
}

public class NodeInformation : NodeAddress
{
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
        return HashCode.Combine(this.Type, this, Engagement, this.Port, this.Address);
    }
}
