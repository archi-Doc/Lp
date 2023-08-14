// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.CrystalDataTest;

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
internal partial record StandardData : IEquatableObject<StandardData>
{
    public StandardData()
    {
    }

    [Key(0, AddProperty = "Id")]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [MaxLength(2)]
    private string name = string.Empty;

    [Key(2, AddProperty = "Age")]
    private double age;

    [Key(3, AddProperty = "Children")]
    private GoshujinClass? children;

    public override string ToString()
        => $"{this.id} {this.name} ({this.age.ToString()})";

    public bool ObjectEquals(StandardData other)
        => this.id == other.id && this.name == other.name && this.age == other.age;
}

public class StandardDataTest
{
    [Fact]
    public async Task TestSerializable()
    {
        var c = await TestHelper.CreateAndStartCrystal<StandardData.GoshujinClass>();

        // g1: empty
        var g1 = c.Data;

        await c.Save(true);
        await c.Crystalizer.SaveJournal();

        // g2: Zero
        await c.PrepareAndLoad(false);
        var g2 = c.Data;
        g2.GoshujinEquals(g1).IsTrue();

        g2.Count.Is(0);
        using (var w = g2.TryLock(0, TryLockMode.GetOrCreate)!)
        {
            w.Name = "Zero";
            w.Commit();
        }

        await c.Save(true);
        await c.Crystalizer.SaveJournal();
        var result = await c.Crystalizer.TestJournalAll();
        result.IsTrue();

        // g3: Zero
        await c.PrepareAndLoad(false);
        var g3 = c.Data;
        g3.GoshujinEquals(g2).IsTrue();

        var d = g3.TryGet(0)!;
        d.Id.Is(0);
        d.Name.Is("Ze");
        d.Age.Is(0d);
        d.Children.IsNull();

        await TestHelper.UnloadAndDeleteAll(c);
    }
}
