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
    public static new NodeInformation Alternative
    {
        get
        {
            if (alternative == null)
            {
                alternative = new NodeInformation(NodeAddress.Alternative);
                alternative.UpdateTime = Ticks.GetUtcNow();
                alternative.PublicKeyX = NodePrivateKey.AlternativePrivateKey.X;
                alternative.PublicKeyY = NodePrivateKey.AlternativePrivateKey.Y;
            }

            return alternative;
        }
    }

    public NodeInformation()
    {
    }

    public NodeInformation(NodeAddress nodeAddress)
        : base(nodeAddress.Address, nodeAddress.Port)
    {
    }

    [Key(4)]
    public long UpdateTime { get; internal protected set; }

    [Key(5)]
    public ulong Differentiation { get; protected set; }

    [Key(6)]
    public byte[] PublicKeyX { get; internal protected set; } = default!;

    [Key(7)]
    public byte[] PublicKeyY { get; internal protected set; } = default!;

    public bool Equals(NodeInformation? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Engagement == other.Engagement && this.Port == other.Port && this.Address.Equals(other.Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Engagement, this.Port, this.Address);
    }

    private static NodeInformation? alternative;
}
