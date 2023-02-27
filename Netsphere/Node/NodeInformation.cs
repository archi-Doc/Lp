// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace Netsphere;

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
                alternative.UpdateTime = Mics.GetUtcNow();
                alternative.PublicKey = NodePrivateKey.AlternativePrivateKey.ToPublicKey();
            }

            return alternative;
        }
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out NodeInformation? nodeInformation)
    {
        nodeInformation = null;
        if (!NodeAddress.TryParse(text, out var nodeAddress, out var publicKeySpan))
        {
            return false;
        }

        if (!NodePublicKey.TryParse(publicKeySpan, out var publicKey))
        {
            return false;
        }

        nodeInformation = new(nodeAddress);
        nodeInformation.PublicKey = publicKey;
        return true;
    }

    public static NodeInformation Merge(NodeAddress nodeAddress, NodeInformation nodeInformation)
    {
        var x = TinyhandSerializer.Clone(nodeInformation);
        x.Engagement = nodeAddress.Engagement;
        x.Port = nodeAddress.Port;
        x.Address = nodeAddress.Address;

        return x;
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
    public NodePublicKey PublicKey { get; internal protected set; } = default!;

    public bool Equals(NodeInformation? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Engagement == other.Engagement && this.Port == other.Port && this.Address.Equals(other.Address);
    }

    public override string ToString()
    {
        if (this.Address.Equals(IPAddress.None))
        {
            return $"None:{this.Port}({this.PublicKey.ToString()})";
        }
        else
        {
            return $"{this.Address}:{this.Port}({this.PublicKey.ToString()})";
        }
    }

    public string ToShortString()
    {
        if (this.Address.Equals(IPAddress.None))
        {
            return $"None:{this.Port}";
        }
        else
        {
            return $"{this.Address}:{this.Port}";
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Engagement, this.Port, this.Address);
    }

    private static NodeInformation? alternative;
}
