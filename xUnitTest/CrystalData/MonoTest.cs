// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using LP;
using Tinyhand;
using Xunit;

namespace xUnitTest.CrystalDataTest;

[TinyhandObject]
public partial class TestData : IMonoData
{
    public TestData()
    {
    }

    public TestData(int id)
    {
        this.Id = id;
        this.Name = id.ToString();
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}

public class MonoTest
{
    public MonoTest()
    {
    }

    [Fact]
    public async Task Test1()
    {
        var mono = new Mono();
        mono.Register(new Mono<Identifier>.StandardGroup<TestData>(10));

        var group = mono.GetGroup<TestData>();
        group.IsNotNull();
        var result = group.TryGet(new(0), out var data);
        result.IsFalse();

        group.Set(new(0), new(0));
        result = group.TryGet(new(0), out data);
        result.IsTrue();

        for (var i = 0; i < 12; i++)
        {
            group.Set(new(i), new(i));
        }

        group.TryGet(new(0), out data).IsFalse();
        group.TryGet(new(1), out data).IsFalse();
        group.TryGet(new(2), out data).IsTrue();
        group.TryGet(new(3), out data).IsTrue();

        group.Remove(new(3));
        group.TryGet(new(3), out data).IsFalse();

        var b = mono.Serialize<TestData>();
        mono.Deserialize<TestData>(b);

        group.TryGet(new(0), out data).IsFalse();
        group.TryGet(new(1), out data).IsFalse();
        group.TryGet(new(2), out data).IsTrue();
        group.TryGet(new(3), out data).IsFalse();

        for (var i = 4; i < 12; i++)
        {
            group.TryGet(new(i), out data).IsTrue();
        }
    }

    [Fact]
    public async Task Test2()
    {
    }
}
