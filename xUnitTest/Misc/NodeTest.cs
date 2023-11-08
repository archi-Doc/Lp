// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Arc.Crypto;
using LP.T3CS;
using Netsphere;
using Xunit;

namespace xUnitTest;

public class NodeTest
{
    [Fact]
    public void DualAddress1()
    {
        TestDualAddress("192.168.0.0:49152", false).IsTrue();
        TestDualAddress("192.168.0.1:49152", false).IsTrue();
        TestDualAddress("0.0.0.0:49152", false).IsTrue();
        TestDualAddress("10.1.2.3:49152", false).IsTrue();
        TestDualAddress("100.64.1.2:49152", false).IsTrue();
        TestDualAddress("127.0.0.0:49152", false).IsTrue();
        TestDualAddress("172.30.5.4:49152", false).IsTrue();
        TestDualAddress("192.0.1.1:49152", true).IsTrue();
        TestDualAddress("192.0.0.5:49152", false).IsTrue();
        TestDualAddress("172.217.25.228:49152", true).IsTrue();

        TestDualAddress("[::]:49152", false).IsTrue();
        TestDualAddress("[::1]:49152", false).IsTrue();
        TestDualAddress("[fe80::]:49152", false).IsTrue();
        TestDualAddress("[fe8b::]:49152", false).IsTrue();
        TestDualAddress("[febc:1111::]:49152", false).IsTrue();
        TestDualAddress("[fecd:1111::]:49152", true).IsTrue();
        TestDualAddress("[fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff]:49152", false).IsTrue();
        TestDualAddress("[fc00::]:49152", false).IsTrue();
        TestDualAddress("[2404:6800:4004:80a::2004]:49152", true).IsTrue();

        TestDualAddress("172.217.25.228:49152[2404:6800:4004:80a::2004]:49152", true).IsTrue();
        TestDualAddress("2404:6800:4004:80a::2004:49152", true, false).IsTrue();
    }

    [Fact]
    public void DualAddressAndPublicKey1()
    {
        TestDualNode("127.0.0.1:49999(CFb2uYv8BdB41DV0o3QXn8pKjwYRdqCY6peXyuPc-a8m)", false).IsTrue();
    }

    [Fact]
    public void DualAddressAndPublicKeyRandom()
    {
        const int N = 10;
        var r = RandomVault.Pseudo;

        for (var i = 0; i < N; i++)
        {
            GenerateDualNode(r, 0, out var address);
            TestDualNode(address).IsTrue();
        }

        for (var i = 0; i < N; i++)
        {
            GenerateDualNode(r, 1,  out var address);
            TestDualNode(address).IsTrue();
        }

        for (var i = 0; i < N; i++)
        {
            GenerateDualNode(r, 2, out var address);
            TestDualNode(address).IsTrue();
        }
    }

    [Fact]
    public void IsValidIPv4()
    {
        this.CreateAddress("192.168.0.0").IsValid().IsFalse();
        this.CreateAddress("192.168.0.1").IsValid().IsFalse();
        this.CreateAddress("0.0.0.0").IsValid().IsFalse();
        this.CreateAddress("10.1.2.3").IsValid().IsFalse();
        this.CreateAddress("100.64.1.2").IsValid().IsFalse();
        this.CreateAddress("127.0.0.0").IsValid().IsFalse();
        this.CreateAddress("172.30.5.4").IsValid().IsFalse();
        this.CreateAddress("192.0.1.1").IsValid().IsTrue();
        this.CreateAddress("192.0.0.5").IsValid().IsFalse();
        this.CreateAddress("172.217.25.228").IsValid().IsTrue();
    }

    [Fact]
    public void IsValidIPv6()
    {
        this.CreateAddress("::").IsValid().IsFalse();
        this.CreateAddress("::1").IsValid().IsFalse();
        this.CreateAddress("fe80::").IsValid().IsFalse();
        this.CreateAddress("fe8b::").IsValid().IsFalse();
        this.CreateAddress("febc:1111::").IsValid().IsFalse();
        this.CreateAddress("fecd:1111::").IsValid().IsTrue();
        this.CreateAddress("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff").IsValid().IsFalse();
        this.CreateAddress("fc00::").IsValid().IsFalse();
        this.CreateAddress("2404:6800:4004:80a::2004").IsValid().IsTrue();
    }

    private static bool TestDualAddress(string utf16, bool validation, bool compareUtf16 = true)
    {
        DualAddress.TryParse(utf16, out var address).IsTrue();

        Span<char> destination = stackalloc char[DualAddress.MaxStringLength];
        address.TryFormat(destination, out var written).IsTrue();
        destination = destination.Slice(0, written);

        if (compareUtf16)
        {
            utf16.Is(destination.ToString());
        }

        DualAddress.TryParse(destination, out var address2).IsTrue();
        address2.Equals(address).IsTrue();

        address.Validate().Is(validation);

        return true;
    }

    private static bool TestDualNode(string utf16, bool validation, bool compareUtf16 = true)
    {
        DualNode.TryParse(utf16, out var addressAndKey).IsTrue();

        Span<char> destination = stackalloc char[DualNode.MaxStringLength];
        addressAndKey.TryFormat(destination, out var written).IsTrue();
        destination = destination.Slice(0, written);

        if (compareUtf16)
        {
            utf16.Is(destination.ToString());
        }

        DualNode.TryParse(destination, out var addressAndKey2).IsTrue();
        addressAndKey2.Equals(addressAndKey).IsTrue();

        addressAndKey.Validate().Is(validation);

        return true;
    }

    private static bool TestDualNode(DualNode addressAndKey)
    {
        Span<char> destination = stackalloc char[DualNode.MaxStringLength];
        addressAndKey.TryFormat(destination, out var written).IsTrue();
        destination = destination.Slice(0, written);

        DualNode.TryParse(destination, out var addressAndKey2).IsTrue();
        addressAndKey2.Equals(addressAndKey).IsTrue();

        // addressAndKey.Validate().Is(true);

        return true;
    }

    private static void GenerateDualNode(RandomVault r, int type, out DualNode addressAndKey)
    {
        var key = NodePrivateKey.Create();

        if (type == 0)
        {// IPv4
            var port4 = (ushort)r.NextInt32(NetControl.MinPort, NetControl.MaxPort);
            var address4 = r.NextUInt32();
            addressAndKey = new(new(port4, address4, 0, 0, 0), key.ToPublicKey());
        }
        else if (type == 1)
        {// IPv6
            var port6 = (ushort)r.NextInt32(NetControl.MinPort, NetControl.MaxPort);
            var address6a = r.NextUInt64();
            var address6b = r.NextUInt64();
            addressAndKey = new(new(0, 0, port6, address6a, address6b), key.ToPublicKey());
        }
        else
        {
            var port4 = (ushort)r.NextInt32(NetControl.MinPort, NetControl.MaxPort);
            var address4 = r.NextUInt32();
            var port6 = (ushort)r.NextInt32(NetControl.MinPort, NetControl.MaxPort);
            var address6a = r.NextUInt64();
            var address6b = r.NextUInt64();
            addressAndKey = new(new(port4, address4, port6, address6a, address6b), key.ToPublicKey());
        }
    }

    private NodeAddress CreateAddress(string address) => new NodeAddress(IPAddress.Parse(address), NetControl.MinPort);
}
