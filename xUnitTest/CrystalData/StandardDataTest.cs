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

    public StandardData(int id, string name, double age)
    {
        this.id = id;
        this.name = name;
        this.age = age;
    }

    [Key(0, AddProperty = "Id")]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    private int id;

    [Key(1, AddProperty = "Name")]
    private string name = string.Empty;

    [Key(2, AddProperty = "Age")]
    private double age;

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
        var g1 = c.Data;
        lock (g1.SyncObject)
        {
        }

        await c.Save(true);
        await c.Crystalizer.SaveJournal();

        // g2: empty
        await c.PrepareAndLoad(false);
        var g2 = c.Data;
        g2.GoshujinEquals(g1).IsTrue();

        lock (g2.SyncObject)
        {
            g2.Count.Is(0);

            g2.Add(new(0, "Zero", 0));
        }

        await c.Save(true);
        await c.Crystalizer.SaveJournal();
        var result = await c.Crystalizer.TestJournalAll();
        result.IsTrue();

        await TestHelper.UnloadAndDeleteAll(c);
    }
}
