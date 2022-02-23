// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Xunit;
using ZenItz;

namespace xUnitTest.Zen;

// [Collection(ZenFixtureCollection.Name)]
public class BasicTest
{
    public BasicTest()
    {
        // this.ZenFixture = zenFixture;
    }

    [Fact]
    public async Task Test0()
    {
        var zen = new ZenFixture().ZenControl.Zen;

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
        // f.IsNotNull();
        f = zen.TryCreateOrGet(Identifier.Zero);
    }

    [Fact]
    public async Task Test1()
    {
        var zen = new ZenFixture().ZenControl.Zen;

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();

        f = zen.TryCreateOrGet(Identifier.Zero);
        f.IsNotNull();

        // Thread.Sleep(3000);
    }

    [Fact]
    public async Task Test2()
    {
        var zen = new ZenFixture().ZenControl.Zen;

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
        // f.IsNotNull();
    }

    // public ZenFixture ZenFixture { get; }
}
