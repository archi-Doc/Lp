// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

/// <summary>
/// Represents a node information.<br/>
/// UpdateTime, Differentiation, PublicKey.
/// </summary>
[TinyhandObject]
public partial class NodeInformation : NodeAddress, IEquatable<NodeInformation>
{
    public NodeInformation()
    {
    }

    [Key(4)]
    public ulong UpdateTime { get; protected set; }

    [Key(5)]
    public ulong Differentiation { get; protected set; }

    [Key(6)]
    public byte[] PublicKeyX { get; protected set; } = default!;

    [Key(7)]
    public byte[] PublicKeyY { get; protected set; } = default!;

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
