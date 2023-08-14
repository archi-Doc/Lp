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
        this.Id = id;
        this.Name = name;
        this.Age = age;
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
    {
        if (this.id != other.id)
        {
            return false;
        }

        if (this.name != other.name)
        {
            return false;
        }

        if (this.age != other.age)
        {
            return false;
        }

        if (this.children is null)
        {
            if (other.children is null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (other.children is null)
            {
                return false;
            }
            else
            {
                return this.children.GoshujinEquals(other.children);
            }
        }
    }
}

public class StandardDataTest
{
    [Fact]
    public async Task Test1()
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
        g2.Add(new(0, "Zero", 0));
        /*using (var w = g2.TryLock(0, TryLockMode.GetOrCreate)!)
        {
            w.Name = "Zero";
            w.Commit();
        }*/

        await c.Save(true);
        await c.Crystalizer.SaveJournal();
        var result = await c.Crystalizer.TestJournalAll();
        result.IsTrue();

        // g3: Zero, 1, 2
        await c.PrepareAndLoad(false);
        var g3 = c.Data;
        g3.GoshujinEquals(g2).IsTrue();

        var d = g3.TryGet(0)!;
        d.Id.Is(0);
        d.Name.Is("Ze");
        d.Age.Is(0d);
        d.Children.IsNull();
        using (var w = g3.TryLock(1, TryLockMode.GetOrCreate)!)
        {
            w.Name = "1";
            w.Children = new();
            w.Commit();
        }

        using (var w = g3.TryLock(2, TryLockMode.GetOrCreate)!)
        {
            w.Name = "2";
            w.Children = new();
            using (var w2 = w.Children.TryLock(22, TryLockMode.GetOrCreate)!)
            {
                w2.Name = "22";
                w2.Commit();
            }

            w.Commit();
        }

        using (var w = g3.TryLock(2, TryLockMode.GetOrCreate)!)
        {
            using (var w2 = w.Children!.TryLock(33, TryLockMode.GetOrCreate)!)
            {
                w2.Name = "33";
                w2.Commit();
            }

            w.Commit();
        }

        await c.Save(true);
        await c.Crystalizer.SaveJournal();
        result = await c.Crystalizer.TestJournalAll();
        result.IsTrue();

        // g4
        await c.PrepareAndLoad(false);
        var g4 = c.Data;
        g4.GoshujinEquals(g3).IsTrue();

        await TestHelper.UnloadAndDeleteAll(c);
    }
}
