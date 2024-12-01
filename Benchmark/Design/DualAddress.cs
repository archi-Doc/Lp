﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Arc.Crypto;
using Tinyhand;

namespace Netsphere;

[TinyhandObject]
public readonly partial record struct DualAddressRecord
{
    [Key(0)]
    public readonly ushort Engagement4;

    [Key(1)]
    public readonly ushort Engagement6;

    [Key(2)]
    public readonly ushort Port4;

    [Key(3)]
    public readonly ushort Port6;

    [Key(4)]
    public readonly uint Address4;

    [Key(5)]
    public readonly ulong Address6A;

    [Key(6)]
    public readonly ulong Address6B;

    public DualAddressRecord(ushort port4, uint address4, ushort port6, ulong address6a, ulong address6b)
    {
        this.Port4 = port4;
        this.Port6 = port6;
        this.Address4 = address4;
        this.Address6A = address6a;
        this.Address6B = address6b;
    }
}

[TinyhandObject]
public readonly partial struct DualAddressImplemented : IEquatable<DualAddressImplemented>
{
    [Key(0)]
    public readonly ushort RelayId4;

    [Key(1)]
    public readonly ushort RelayId6;

    [Key(2)]
    public readonly ushort Port4;

    [Key(3)]
    public readonly ushort Port6;

    [Key(4)]
    public readonly uint Address4;

    [Key(5)]
    public readonly ulong Address6A;

    [Key(6)]
    public readonly ulong Address6B;

    public DualAddressImplemented(ushort port4, uint address4, ushort port6, ulong address6a, ulong address6b)
    {
        this.Port4 = port4;
        this.Port6 = port6;
        this.Address4 = address4;
        this.Address6A = address6a;
        this.Address6B = address6b;
    }

    public bool Equals(DualAddressImplemented other)
        => this.RelayId4 == other.RelayId4 &&
        this.RelayId6 == other.RelayId6 &&
        this.Port4 == other.Port4 &&
        this.Port6 == other.Port6 &&
        this.Address4 == other.Address4 &&
        this.Address6A == other.Address6A &&
        this.Address6B == other.Address6B;

    public override int GetHashCode()
        => HashCode.Combine(this.RelayId4, this.RelayId6, this.Port4, this.Port6, this.Address4, this.Address6A, this.Address6B);
}

/// <summary>
/// Represents ipv4/ipv6 address information.<br/>
/// Determine if the address is valid based on whether the port number is greater than zero.
/// </summary>
[TinyhandObject]
public readonly partial struct DualAddress : IStringConvertible<DualAddress>
{
    [Key(0)]
    public readonly ushort RelayId4;

    [Key(1)]
    public readonly ushort RelayId6;

    [Key(2)]
    public readonly ushort Port4;

    [Key(3)]
    public readonly ushort Port6;

    [Key(4)]
    public readonly uint Address4;

    [Key(5)]
    public readonly ulong Address6A;

    [Key(6)]
    public readonly ulong Address6B;

    public DualAddress(ushort port4, uint address4, ushort port6, ulong address6a, ulong address6b)
    {
        this.Port4 = port4;
        this.Port6 = port6;
        this.Address4 = address4;
        this.Address6A = address6a;
        this.Address6B = address6b;
    }

    public bool IsValidIpv4 => this.Port4 != 0;

    public bool IsValidIpv6 => this.Port6 != 0;

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out DualAddress instance, out int read)
    {// 1.2.3.4:55, []:55, 1.2.3.4:55[]:55
        ushort port4 = 0;
        ushort port6 = 0;
        uint address4 = 0;
        ulong address6a = 0;
        ulong address6b = 0;

        instance = default;
        read = 0;

        source = source.Trim();
        if (source.Length == 0)
        {
            return false;
        }
        else if (IsIpv4Address(source))
        {// TryParse IPv4
            TryParseIPv4(ref source, out port4, out address4);
        }

        // TryParse IPv6
        TryParseIPv6(ref source, out port6, out address6a, out address6b);

        instance = new(port4, address4, port6, address6a, address6b);
        read = source.Length;//Fix
        return true;
    }

    public static int MaxStringLength
        => (15 + 1 + 5) + (2 + 54 + 1 + 5); // IPv4:12345, [IPv6]:12345

    public int GetStringLength()
        => -1;

    public bool TryFormat(Span<char> destination, out int written)
    {// 15 + 1 + 5, 54 + 1 + 5 + 2
        if (destination.Length < MaxStringLength)
        {
            written = 0;
            return false;
        }

        var span = destination;
        if (this.IsValidIpv4)
        {
            Span<byte> ipv4byte = stackalloc byte[4];
            BitConverter.TryWriteBytes(ipv4byte, this.Address4);
            var ipv4 = new IPAddress(ipv4byte);
            if (!ipv4.TryFormat(span, out written))
            {
                return false;
            }

            span = span.Slice(written);

            span[0] = ':';
            span = span.Slice(1);
            this.Port4.TryFormat(span, out written);
            span = span.Slice(written);
        }

        if (this.IsValidIpv6)
        {
            span[0] = '[';
            span = span.Slice(1);

            Span<byte> ipv6byte = stackalloc byte[16];
            BitConverter.TryWriteBytes(ipv6byte, this.Address6A);
            BitConverter.TryWriteBytes(ipv6byte.Slice(sizeof(ulong)), this.Address6B);
            var ipv6 = new IPAddress(ipv6byte);
            if (!ipv6.TryFormat(span, out written))
            {
                return false;
            }

            span = span.Slice(written);

            span[0] = ']';
            span[1] = ':';
            span = span.Slice(2);

            this.Port6.TryFormat(span, out written);
            span = span.Slice(written);
        }

        written = destination.Length - span.Length;
        return true;
    }

    public override string ToString()
    {
        Span<char> span = stackalloc char[MaxStringLength];
        return this.TryFormat(span, out var written) ? span.Slice(0, written).ToString() : string.Empty;
    }

    private static bool TryParseIPv4(ref ReadOnlySpan<char> source, out ushort port4, out uint address4)
    {
        port4 = 0;
        address4 = 0;

        var index = source.IndexOf(':');
        if (index < 0)
        {
            return false;
        }

        var sourceAddress = source.Slice(0, index); // "1.2.3.4"
        ReadOnlySpan<char> sourcePort;
        source = source.Slice(index + 1); // :"xxxx"
        index = source.IndexOf('[');
        if (index < 0)
        {// Only IPv4
            sourcePort = source;
            source = ReadOnlySpan<char>.Empty;
        }
        else
        {
            sourcePort = source.Slice(0, index); // "xxx"[
            source = source.Slice(index); // "[xxxx]"
        }

        if (!IPAddress.TryParse(sourceAddress, out var ipAddress) ||
            ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        Span<byte> span = stackalloc byte[4];
        if (!ipAddress.TryWriteBytes(span, out _))
        {
            return false;
        }

        address4 = BitConverter.ToUInt32(span);

        if (!ushort.TryParse(sourcePort, out port4))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseIPv6(ref ReadOnlySpan<char> source, out ushort port6, out ulong address6a, out ulong address6b)
    {// [ipv6 address]:port
        port6 = 0;
        address6a = 0;
        address6b = 0;

        if (source.Length == 0)
        {
            return false;
        }

        ReadOnlySpan<char> sourceAddress;
        ReadOnlySpan<char> sourcePort;
        int index;
        if (source[0] == '[')
        {// [123::1]:Port
            index = source.IndexOf(']');
            if (index < 0)
            {
                return false;
            }

            sourceAddress = source.Slice(1, index - 1); // "123::1"
            source = source.Slice(index + 1);
            index = source.IndexOf(':');
            if (index < 0)
            {
                return false;
            }

            sourcePort = source.Slice(index + 1); // :"xxxx"
        }
        else
        {// 123::1:Port
            index = source.LastIndexOf(':');
            if (index < 0)
            {
                return false;
            }

            sourceAddress = source.Slice(0, index);
            sourcePort = source.Slice(index + 1);
        }

        source = ReadOnlySpan<char>.Empty;

        if (!IPAddress.TryParse(sourceAddress, out var ipAddress) ||
            ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return false;
        }

        Span<byte> span = stackalloc byte[16];
        if (!ipAddress.TryWriteBytes(span, out _))
        {
            return false;
        }

        address6a = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        address6b = BitConverter.ToUInt64(span);

        if (!ushort.TryParse(sourcePort, out port6))
        {
            return false;
        }

        return true;
    }

    private static bool IsIpv4Address(ReadOnlySpan<char> source)
    {
        if (source.Length == 0)
        {
            return false;
        }
        else if (source[0] == '[')
        {// IPv6
            return false;
        }

        foreach (var x in source)
        {
            if (x == '.')
            {// IPv4
                return true;
            }
            else if (x == ':')
            {// IPv6
                return false;
            }
        }

        return false; // Unknown
    }
}
