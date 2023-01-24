// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Xunit;
using ZenItz;

namespace xUnitTest.ZenTest;

public class BasicTest
{
    public BasicTest()
    {
    }

    [Fact]
    public async Task Test1()
    {
        var identifier = default(Identifier);
        var zen = TestHelper.CreateZen<Identifier>();

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
        f = zen.TryCreateOrGet(Identifier.Zero);
        f.IsNotNull();

        var buffer = new byte[Identifier.Length];
        var buffer2 = new byte[Identifier.Length];
        Identifier.Zero.TryWriteBytes(buffer);
        f!.Set(buffer);
        var result = await f!.Get();
        result.DataEquals(buffer).IsTrue();

        // Set flakes
        for (var i = 1; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = zen.TryCreateOrGet(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            f!.Set(buffer).Is(ZenResult.Success);
        }

        // Get flakes and check
        for (var i = 0; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = zen.TryGet(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            result = await f!.Get();
            result.DataEquals(buffer).IsTrue();
        }

        f = zen.TryCreateOrGet(Identifier.Zero);
        f.IsNotNull();

        // Set fragments
        for (var i = 0; i < Zen.MaxFragmentCount; i++)
        {
            identifier = new Identifier(i);
            identifier.TryWriteBytes(buffer);

            f!.SetFragment(identifier, buffer).Is(ZenResult.Success);
        }

        identifier = new Identifier(Zen.MaxFragmentCount);
        identifier.TryWriteBytes(buffer);

        f!.SetFragment(identifier, buffer).Is(ZenResult.OverNumberLimit);
    }

    [Fact]
    public async Task Test2()
    {
        var zen = TestHelper.CreateZen<Identifier>();

        var f = zen.TryGet(Identifier.Zero);
        f.IsNull();
    }

    // public ZenFixture ZenFixture { get; }
}
