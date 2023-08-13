// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using CrystalData;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.CrystalDataTest;

[TinyhandObject(Journaling = true)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial record SerializableData
{
    public SerializableData()
    {
    }

    public SerializableData(int id, string name, double age)
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
}

public class JournalTest
{
    [Fact]
    public async Task TestSerializable()
    {
        // var tester = new JournalTester();
        var c = await this.CreateAndStartCrystal();
        var g1 = c.Data;
        lock (g1.SyncObject)
        {
        }

        await c.Save(true);

        // g2: empty
        await c.PrepareAndLoad(false);
        var g2 = c.Data;
        lock (g2.SyncObject)
        {
            g2.Count.Is(0);

            g2.Add(new(0, "Zero", 0));
        }

        await c.Save(true);

        // g3: Zero
        await c.PrepareAndLoad(false);
        var g3 = c.Data;
        lock (g3.SyncObject)
        {
            g3.Count.Is(1);
            var d = g3.IdChain.FindFirst(0)!;
            d.IsNotNull();
            d.Id.Is(0);
            d.Name.Is("Zero");
            d.Age.Is(0d);
        }

        await c.Save(true);
        c.Crystalizer.TestJournalAll();

        await TestHelper.UnloadAndDeleteAll(c);
    }

    private async Task<ICrystal<SerializableData.GoshujinClass>> CreateAndStartCrystal()
    {
        var builder = new CrystalControl.Builder();
        builder
            .ConfigureCrystal(context =>
            {
                var directory = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
                context.AddCrystal<SerializableData.GoshujinClass>(
                    new(SavePolicy.Manual, new LocalFileConfiguration(Path.Combine(directory, "Test.tinyhand")))
                    {
                        SaveFormat = SaveFormat.Utf8,
                        NumberOfHistoryFiles = 5,
                    });
            });

        var unit = builder.Build();
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();

        var crystal = crystalizer.GetCrystal<SerializableData.GoshujinClass>();
        var result = await crystal.PrepareAndLoad(false);
        result.Is(CrystalResult.Success);
        return crystal;
    }
}
