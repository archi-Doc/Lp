// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Xunit;
using ZenItz;

namespace xUnitTest.Zen;

[Collection(TestZenCollection.Name)]
public class BasicTest
{
    public BasicTest(TestZen testZen)
    {
        this.TestZen = testZen;
    }

    [Fact]
    public async Task Test1()
    {
        var zen = this.ZenControl.Zen;

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
        // f.IsNotNull();
    }

    [Fact]
    public async Task Test2()
    {
        var zen = this.ZenControl.Zen;

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
        // f.IsNotNull();
    }

    public TestZen TestZen { get; }

    public ZenControl ZenControl => this.TestZen.ZenControl;
}
