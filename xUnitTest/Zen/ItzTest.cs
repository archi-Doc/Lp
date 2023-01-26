// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Tinyhand;
using Xunit;
using ZenItz;

namespace xUnitTest.ZenTest;

[TinyhandObject]
public partial class TestPayload : IPayload
{
    public TestPayload()
    {
    }

    public TestPayload(int id)
    {
        this.Id = id;
        this.Name = id.ToString();
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}

public class ItzTest
{
    public ItzTest()
    {
    }

    [Fact]
    public async Task Test1()
    {
        var itz = new Itz();
        itz.RegisterShip(new Itz<Identifier>.DefaultShip<TestPayload>(10));

        var ship = itz.GetShip<TestPayload>();
        ship.IsNotNull();
        var result = ship.TryGet(new(0), out var payload);
        result.IsFalse();

        ship.Set(new(0), new(0));
        result = ship.TryGet(new(0), out payload);
        result.IsTrue();

        for (var i = 0; i < 12; i++)
        {
            ship.Set(new(i), new(i));
        }

        ship.TryGet(new(0), out payload).IsFalse();
        ship.TryGet(new(1), out payload).IsFalse();
        ship.TryGet(new(2), out payload).IsTrue();
        ship.TryGet(new(3), out payload).IsTrue();

        ship.Remove(new(3));
        ship.TryGet(new(3), out payload).IsFalse();

        var b = itz.Serialize<TestPayload>();
        itz.Deserialize<TestPayload>(b);

        ship.TryGet(new(0), out payload).IsFalse();
        ship.TryGet(new(1), out payload).IsFalse();
        ship.TryGet(new(2), out payload).IsTrue();
        ship.TryGet(new(3), out payload).IsFalse();

        for (var i = 4; i < 12; i++)
        {
            ship.TryGet(new(i), out payload).IsTrue();
        }
    }

    [Fact]
    public async Task Test2()
    {
    }
}
