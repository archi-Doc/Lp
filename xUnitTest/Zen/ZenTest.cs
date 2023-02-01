// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP;
using Tinyhand;
using Xunit;
using Xunit.Sdk;
using ZenItz;

namespace xUnitTest.ZenTest;

public partial class ZenTest
{
    public const int N = 100;

    public ZenTest()
    {
    }

    [Fact]
    public async Task TestTemplate()
    {
        // var zen = await TestHelper.CreateAndStartZen<Identifier>();

        // await TestHelper.StopZen(zen);
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
        var root = zen.Root;
        Zen.Flake? flake;
        root.Remove().IsFalse();

        var bin = new byte[N];
        LP.Random.Pseudo.NextBytes(bin);

        // Set flakes
        for (var i = 0; i < N; i++)
        {
            flake = root.GetOrCreateChild(new(i));
            flake.SetData(bin.AsSpan(0, i));
        }

        // Get flakes and check
        for (var i = 0; i < N; i++)
        {
            flake = zen.Root.TryGetChild(new(i));
            flake.IsNotNull();

            var result = await flake!.GetData();
            result.DataEquals(bin.AsSpan(0, i)).IsTrue();
        }

        await TestHelper.StopAndStartZen(zen);

        // Get flakes and check
        for (var i = 0; i < N; i++)
        {
            flake = zen.Root.TryGetChild(new(i));
            flake.IsNotNull();

            var result = await flake!.GetData();
            result.DataEquals(bin.AsSpan(0, i)).IsTrue();
        }

        await TestHelper.StopZen(zen);
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    internal partial record TestObject(int Id, string Name);

    [Fact]
    public async Task Test3()
    {
        var zen = await TestHelper.CreateAndStartZen<Identifier>();
        var root = zen.Root;

        var t1 = new TestObject(1, "1");
        var t2 = new TestObject(2, "2");
        var t3 = new TestObject(3, "3");

        var flake = root.GetOrCreateChild(new(1));
        flake.SetDataObject(t1);
        var result = await flake.GetDataObject<TestObject>();
        result.Object.IsStructuralEqual(t1);

        flake.SetFragmentObject(new(2), t2);
        result = await flake.GetFragmentObject<TestObject>(new(2));
        result.Object.IsStructuralEqual(t2);

        flake.RemoveFragment(new(3)).IsFalse();
        flake.SetFragmentObject(new(3), t3);
        flake.SetFragment(new(3), TinyhandSerializer.SerializeObject(t3));
        result = await flake.GetFragmentObject<TestObject>(new(3));
        result.Object.IsStructuralEqual(t3);
        flake.RemoveFragment(new(3)).IsTrue();

        await TestHelper.StopAndStartZen(zen);

        result = await flake.GetDataObject<TestObject>();
        result.Object.IsStructuralEqual(t1);
        result = await flake.GetFragmentObject<TestObject>(new(2));
        result.Object.IsStructuralEqual(t2);

        await TestHelper.StopZen(zen);
    }
}
