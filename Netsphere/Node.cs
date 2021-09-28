// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP.Net;

public enum NodeType : byte
{
    Development,
    Release,
}

/// <summary>
/// Represents a basic node information.
/// </summary>
[TinyhandObject]
public partial class NodeAddress : IEquatable<NodeAddress>
{
    public static bool TryParse(string text, [NotNullWhen(true)] out NodeAddress? node)
    {
        string address, port;
        node = null;

        text = text.Trim();
        if (text.StartsWith('['))
        {
            var index = text.IndexOf(']');
            if (index < 0)
            {
                return false;
            }

            address = text.Substring(1, index - 1);
            port = text.Substring(index + 1);
            if (port.StartsWith(':'))
            {
                port = port.Substring(1);
            }
        }
        else
        {
            var index = text.LastIndexOf(':');
            if (index < 0)
            {
                return false;
            }

            address = text.Substring(0, index);
            port = text.Substring(index + 1);
        }

        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return false;
        }

        ushort.TryParse(port, out var nodePort);
        node = new NodeAddress(ipAddress, nodePort);
        return true;
    }

    public NodeAddress()
    {
    }

    public NodeAddress(IPAddress address, ushort port)
    {
        this.Address = address;
        this.Port = port;
    }

    [Key(0)]
    public NodeType Type { get; protected set; }

    [Key(1)]
    public byte Engagement { get; protected set; }

    [Key(2)]
    public ushort Port { get; protected set; }

    [Key(3)]
    public IPAddress Address { get; protected set; } = IPAddress.None;

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

/// <summary>
/// Represents an essential node information.
/// </summary>
[ValueLinkObject]
[TinyhandObject]
public partial class NodeAddressEssential : NodeAddress
{
    [Link(TargetMember = nameof(Address), Type = ChainType.Unordered)]
    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    public NodeAddressEssential()
    {
    }
}

/// <summary>
/// Represents a advanced node information.<br/>
/// UpdateTime, Differentiation.
/// </summary>
[TinyhandObject]
public partial class NodeInformation : NodeAddress, IEquatable<NodeInformation>
{
    public NodeInformation()
    {
    }

    [Key(4)]
    public ulong UpdateTime;

    [Key(5)]
    public ulong Differentiation;

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
