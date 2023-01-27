// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Xunit;
using ZenItz;

namespace xUnitTest.ZenTest;

public class ZenTest
{
    public ZenTest()
    {
    }

    [Fact]
    public async Task Test1()
    {
        var identifier = default(Identifier);
        var zen = await TestHelper.CreateAndStartZen<Identifier>();

        var f = zen.Root.TryGetChild(Identifier.Zero);
        f.IsNull();
        f = zen.Root.GetOrCreateChild(Identifier.Zero);
        f.IsNotNull();

        var buffer = new byte[Identifier.Length];
        var buffer2 = new byte[Identifier.Length];
        Identifier.Zero.TryWriteBytes(buffer);
        f!.SetData(buffer);
        var result = await f!.GetData();
        result.DataEquals(buffer).IsTrue();

        // Set flakes
        for (var i = 1; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = zen.Root.GetOrCreateChild(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            f!.SetData(buffer).Is(ZenResult.Success);
        }

        // Get flakes and check
        for (var i = 0; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = zen.Root.TryGetChild(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            result = await f!.GetData();
            result.DataEquals(buffer).IsTrue();
        }

        f = zen.Root.GetOrCreateChild(Identifier.Zero);
        f.IsNotNull();

        // Set fragments
        for (var i = 0; i < ZenOptions.DefaultMaxFragmentCount; i++)
        {
            identifier = new Identifier(i);
            identifier.TryWriteBytes(buffer);

            f!.SetFragment(identifier, buffer).Is(ZenResult.Success);
        }

        identifier = new Identifier(1);
        identifier.TryWriteBytes(buffer);

        f!.SetFragment(identifier, buffer).Is(ZenResult.OverNumberLimit);

        await TestHelper.StopZen(zen);
    }

    [Fact]
    public async Task Test2()
    {
        var zen = await TestHelper.CreateAndStartZen<Identifier>();

        await TestHelper.StopZen(zen);
    }
}
