// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrystalData;
using LP;
using Tinyhand;
using Xunit;
using Xunit.Sdk;

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
        var crystal = await TestHelper.CreateAndStartZen();

        var f = crystal.Data.TryGetChild(Identifier.Zero);
        f.IsNull();
        f = crystal.Data.GetOrCreateChild(Identifier.Zero);
        f.IsNotNull();

        var buffer = new byte[Identifier.Length];
        var buffer2 = new byte[Identifier.Length];
        Identifier.Zero.TryWriteBytes(buffer);
        f!.BlockDatum.Set(buffer);
        var result = await f!.BlockDatum.Get();
        result.DataEquals(buffer).IsTrue();

        // Set flakes
        for (var i = 1; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = crystal.Data.GetOrCreateChild(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            f!.BlockDatum.Set(buffer).Is(CrystalResult.Success);
        }

        // Get flakes and check
        for (var i = 0; i < 1000; i++)
        {
            identifier = new Identifier(i);

            f = crystal.Data.TryGetChild(identifier);
            f.IsNotNull();

            identifier.TryWriteBytes(buffer);
            result = await f!.BlockDatum.Get();
            result.DataEquals(buffer).IsTrue();
        }

        f = crystal.Data.GetOrCreateChild(Identifier.Zero);
        f.IsNotNull();

        // Set fragments
        for (var i = 0; i < CrystalOptions.DefaultMaxFragmentCount; i++)
        {
            identifier = new Identifier(i);
            identifier.TryWriteBytes(buffer);

            f!.FragmentData.Set(identifier, buffer).Is(CrystalResult.Success);
        }

        identifier = new Identifier(1999);
        identifier.TryWriteBytes(buffer);

        f!.FragmentData.Set(identifier, buffer).Is(CrystalResult.OverNumberLimit);

        await TestHelper.StopZen(crystal);
    }

    [Fact]
    public async Task Test2()
    {
        var crystal = await TestHelper.CreateAndStartZen();
        var data = crystal.Data;
        LpData? flake;
        data.Delete().IsFalse();

        var bin = new byte[N];
        LP.Random.Pseudo.NextBytes(bin);

        // Set flakes
        for (var i = 0; i < N; i++)
        {
            flake = data.GetOrCreateChild(new(i));
            flake.BlockDatum.Set(bin.AsSpan(0, i));
        }

        // Get flakes and check
        for (var i = 0; i < N; i++)
        {
            flake = crystal.Data.TryGetChild(new(i));
            flake.IsNotNull();

            var result = await flake!.BlockDatum.Get();
            result.DataEquals(bin.AsSpan(0, i)).IsTrue();
        }

        await TestHelper.StopAndStartZen(crystal);

        // Get flakes and check
        for (var i = 0; i < N; i++)
        {
            flake = crystal.Data.TryGetChild(new(i));
            flake.IsNotNull();

            var result = await flake!.BlockDatum.Get();
            result.DataEquals(bin.AsSpan(0, i)).IsTrue();
        }

        await TestHelper.StopZen(crystal);
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    internal partial record TestObject(int Id, string Name);

    [Fact]
    public async Task Test3()
    {
        var crystal = await TestHelper.CreateAndStartZen();
        var data = crystal.Data;

        var t1 = new TestObject(1, "1");
        var t2 = new TestObject(2, "2");
        var t3 = new TestObject(3, "3");

        // Set 1
        var flake = data.GetOrCreateChild(new(1));
        flake.BlockDatum.SetObject(t1);
        var result = await flake.BlockDatum.GetObject<TestObject>();
        result.Object.IsStructuralEqual(t1);

        // Set 2
        flake.FragmentData.SetObject(new(2), t2);
        result = await flake.FragmentData.GetObject<TestObject>(new(2));
        result.Object.IsStructuralEqual(t2);

        // Set and remove 3
        flake.FragmentData.Remove(new(3)).IsFalse();
        flake.FragmentData.SetObject(new(3), t3);
        flake.FragmentData.Set(new(3), TinyhandSerializer.SerializeObject(t3));
        result = await flake.FragmentData.GetObject<TestObject>(new(3));
        result.Object.IsStructuralEqual(t3);
        flake.FragmentData.Remove(new(3)).IsTrue();

        // Nested
        var nested = flake.TryGetChild(new(1));
        nested.IsNull();
        nested = flake.GetOrCreateChild(new(1));
        nested.IsNotNull();

        nested.BlockDatum.SetObject(t2);
        nested.FragmentData.Set(new(2), TinyhandSerializer.SerializeObject(t2));

        await TestHelper.StopAndStartZen(crystal);

        result = await flake.BlockDatum.GetObject<TestObject>();
        result.Object.IsStructuralEqual(t1);
        result = await flake.FragmentData.GetObject<TestObject>(new(2));
        result.Object.IsStructuralEqual(t2);

        nested = flake.TryGetChild(new(1))!;
        nested.IsNotNull();
        result = await nested.BlockDatum.GetObject<TestObject>();
        result.Object.IsStructuralEqual(t2);
        result = await flake.FragmentData.GetObject<TestObject>(new(2));
        result.Object.IsStructuralEqual(t2);

        // Lock IO order test
        for (var i = 0; i < 100; i++)
        {
            flake = crystal.Data.GetOrCreateChild(Identifier.Zero);
            flake.BlockDatum.Set(new byte[] { 0, 1, });
            flake.Save(true);
            var fd = await flake.BlockDatum.Get();
            fd.Result.Is(CrystalResult.Success);
        }

        // Remove test
        flake = crystal.Data.GetOrCreateChild(Identifier.Zero);
        flake.Delete().IsTrue();
        flake.BlockDatum.Set(new byte[] { 0, 1, }).Is(CrystalResult.Removed);
        (await flake.BlockDatum.Get()).Result.Is(CrystalResult.Removed);

        await TestHelper.StopZen(crystal);
    }
}
