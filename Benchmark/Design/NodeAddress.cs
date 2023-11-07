// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Tinyhand;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace Netsphere;

/// <summary>
/// Represents a basic node information (Address, Port, Engagement, Type).
/// </summary>
[TinyhandObject]
public partial class NodeAddress : IEquatable<NodeAddress>
{
    public static bool TryParse(string text, [NotNullWhen(true)] out NodeAddress? node)
        => TryParse(text, out node, out _);

    internal static bool TryParse(string text, [NotNullWhen(true)] out NodeAddress? node, out ReadOnlySpan<char> publicKeySpan)
    {// 127.0.0.1:100(public)
        int index;
        ReadOnlySpan<char> address;
        ReadOnlySpan<char> port;

        node = null;
        publicKeySpan = default;

        text = text.Trim();
        var span = text.AsSpan();
        if (span.StartsWith("["))
        {
            index = span.IndexOf(']');
            if (index < 0 || index >= span.Length)
            {
                return false;
            }

            address = span.Slice(1, index - 1);
            span = span.Slice(index + 1);
        }
        else
        {
            index = span.LastIndexOf(':');
            if (index < 0)
            {
                return false;
            }

            address = span.Slice(0, index);
            span = span.Slice(index + 1);
        }

        index = span.IndexOf('(');
        if (index >= 0)
        {
            port = span.Slice(0, index);
            span = span.Slice(index + 1);
            index = span.IndexOf(')');
            if (index >= 0)
            {
                publicKeySpan = span.Slice(0, index);
            }
            else
            {
                publicKeySpan = span;
            }
        }
        else
        {
            port = span;
        }

        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return false;
        }

        ushort.TryParse(port, out var nodePort);
        // byte.TryParse(engagement, out var engage);

        node = new NodeAddress(ipAddress, nodePort, 0);
        return true;
    }

    public NodeAddress()
    {
    }

    public NodeAddress(IPAddress address, ushort port, ushort engagement = 0)
    {
        this.Address = address;
        this.Port = port;
        this.Engagement = engagement;
    }

    [Key(0)]
    public ushort Engagement { get; protected set; }

    [Key(1)]
    public ushort Port { get; protected set; }

    [Key(2)]
    public IPAddress Address { get; protected set; } = IPAddress.None;

    public IPEndPoint CreateEndpoint() => new IPEndPoint(this.Address, this.Port);

    public bool IsLocalLoopbackAddress()
    {
        return this.Address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork => this.IsLocalLoopbackAddressIPv4(),
            System.Net.Sockets.AddressFamily.InterNetworkV6 => this.IsLocalLoopbackAddressIPv6(),
            _ => false,
        };
    }

    public bool IsPrivateOrLocalLoopbackAddress()
    {
        return this.Address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork => this.IsPrivateOrLocalLoopbackAddressIPv4(),
            System.Net.Sockets.AddressFamily.InterNetworkV6 => this.IsLocalLoopbackAddressIPv6(),
            _ => false,
        };
    }

    public bool Equals(NodeAddress? other)
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

    public override string ToString()
    {
        if (this.Address.Equals(IPAddress.None))
        {
            return "None";
        }
        else
        {
            return $"{this.Address}:{this.Port}";
        }
    }

    internal void SetIPEndPoint(IPEndPoint endpoint)
    {
        this.Address = endpoint.Address;
        this.Port = (ushort)endpoint.Port;
    }

    internal void SetAddress(IPAddress address)
    {
        this.Address = address;
    }

    internal void SetPort(ushort port)
    {
        this.Port = port;
    }

    private bool IsLocalLoopbackAddressIPv4()
    {
        Span<byte> address = stackalloc byte[4];
        if (!this.Address.TryWriteBytes(address, out var written) || written < 4)
        {
            return false;
        }

        return address[0] == 127 && address[1] == 0 && address[2] == 0;
    }

    private unsafe bool IsLocalLoopbackAddressIPv6()
    {
        Span<byte> address = stackalloc byte[16];
        if (!this.Address.TryWriteBytes(address, out var written) || written < 16)
        {
            return false;
        }

        fixed (byte* b = address)
        {
            ulong* u = (ulong*)b;
            return u[0] == 0 && u[1] == 0x0100000000000000;
        }
    }

    private bool IsPrivateOrLocalLoopbackAddressIPv4()
    {
        Span<byte> address = stackalloc byte[4];
        if (!this.Address.TryWriteBytes(address, out var written) || written < 4)
        {
            return false;
        }

        if (address[0] == 10 || address[0] == 127)
        {// Private network, loopback addresses.
            return true;
        }
        else if (address[0] == 100)
        {
            if (address[1] >= 64 && address[1] <= 127)
            {// Private network
                return true;
            }
        }
        else if (address[0] == 172)
        {// Private network
            if (address[1] >= 16 && address[1] <= 31)
            {
                return true;
            }
        }
        else if (address[0] == 192)
        {
            if (address[1] == 168)
            {// Private network
                return true;
            }
        }

        return false;
    }

    private bool IsValidIPv4()
    {
        Span<byte> address = stackalloc byte[4];
        if (!this.Address.TryWriteBytes(address, out var written) || written < 4)
        {
            return false;
        }

        if (address[0] == 0 || address[0] == 10 || address[0] == 127)
        {// Current network, Private network, loopback addresses.
            return false;
        }
        else if (address[0] == 100)
        {
            if (address[1] >= 64 && address[1] <= 127)
            {// Private network
                return false;
            }
        }
        else if (address[0] == 169 && address[1] == 254)
        {// Link-local addresses.
            return false;
        }
        else if (address[0] == 172)
        {// Private network
            if (address[1] >= 16 && address[1] <= 31)
            {
                return false;
            }
        }
        else if (address[0] == 192)
        {
            if (address[1] == 0)
            {
                if (address[2] == 0 || address[2] == 2)
                {
                    return false;
                }
            }
            else if (address[1] == 88 && address[2] == 99)
            {
                return false;
            }
            else if (address[1] == 168)
            {// Private network
                return false;
            }
        }
        else if (address[0] == 198)
        {
            if (address[1] == 18 || address[1] == 19)
            {
                return false;
            }
        }

        return true;
    }

    private unsafe bool IsValidIPv6()
    {
        Span<byte> address = stackalloc byte[16];
        if (!this.Address.TryWriteBytes(address, out var written) || written < 16)
        {
            return false;
        }

        fixed (byte* b = address)
        {
            ulong* u = (ulong*)b;
            if (u[0] == 0 && (u[1] == 0 || u[1] == 0x0100000000000000))
            {// Unspecified address, Loopback address
                return false;
            }

            if (b[0] == 0xFC || b[0] == 0xFD)
            {// Unique local address
                return false;
            }
            else if (b[0] == 0xFE)
            {
                if (b[1] >= 0x80 && b[1] <= 0xBF)
                {// Link-local address
                    return false;
                }
            }
        }

        return true;
    }
}
