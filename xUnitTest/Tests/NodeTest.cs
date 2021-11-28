using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LP.Net;
using Xunit;

namespace xUnitTest;

public class NodeTest
{
    [Fact]
    public void IsValidIPv4()
    {
        CreateAddress("192.168.0.0").IsValid().IsFalse();
        CreateAddress("192.168.0.1").IsValid().IsFalse();
        CreateAddress("0.0.0.0").IsValid().IsFalse();
        CreateAddress("10.1.2.3").IsValid().IsFalse();
        CreateAddress("100.64.1.2").IsValid().IsFalse();
        CreateAddress("127.0.0.0").IsValid().IsFalse();
        CreateAddress("172.30.5.4").IsValid().IsFalse();
        CreateAddress("192.0.1.1").IsValid().IsTrue();
        CreateAddress("192.0.0.5").IsValid().IsFalse();
        CreateAddress("172.217.25.228").IsValid().IsTrue();
    }

    [Fact]
    public void IsValidIPv6()
    {
        CreateAddress("::").IsValid().IsFalse();
        CreateAddress("::1").IsValid().IsFalse();
        CreateAddress("fe80::").IsValid().IsFalse();
        CreateAddress("fe8b::").IsValid().IsFalse();
        CreateAddress("febc:1111::").IsValid().IsFalse();
        CreateAddress("fecd:1111::").IsValid().IsTrue();
        CreateAddress("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff").IsValid().IsFalse();
        CreateAddress("fc00::").IsValid().IsFalse();
        CreateAddress("2404:6800:4004:80a::2004").IsValid().IsTrue();
    }

    public NodeAddress CreateAddress(string address) => new NodeAddress(IPAddress.Parse(address), NetControl.MinPort);
}
