// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Netsphere;
using Xunit;

namespace xUnitTest;

public class NodeTest
{
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

    public NodeAddress CreateAddress(string address) => new NodeAddress(IPAddress.Parse(address), NetControl.MinPort);
}
