// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using Tinyhand;
using ValueLink;

namespace CrystalDataTest;

[TinyhandObject(Tree = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record TestClass
{// This is it. This class is the crystal of the most advanced data management architecture I've reached so far.
    public static void Register(IUnitCrystalContext context)
    {
        context.AddCrystal<TestClass.GoshujinClass>(
            new()
            {
                SaveFormat = SaveFormat.Binary,
                SavePolicy = SavePolicy.Manual,
                FileConfiguration = new GlobalFileConfiguration("TestClass/Main"),
                // BackupFileConfiguration = new GlobalFileConfiguration("TestClass.tinyhand"),
                StorageConfiguration = GlobalStorageConfiguration.Default,
                NumberOfFileHistories = 2,
            });
    }

    public TestClass()
    {
    }

    [Key(0, AddProperty = "Id", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [Link(Type = ChainType.Ordered)]
    private string name = string.Empty;

    [Key(2, AddProperty = "Child", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<TestClass> child = new();

    [Key(3, AddProperty = "Children", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<TestClass.GoshujinClass> children = new();

    [Key(4, AddProperty = "ByteArray", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<byte[]> byteArray = new();
}

[SimpleCommand("crystaltest")]
public class CrystalTestSubcommand : ISimpleCommandAsync<CrystalTestOptions>
{
    public CrystalTestSubcommand(CrystalControl crystalControl, ICrystal<TestClass.GoshujinClass> crystal)
    {
        this.CrystalControl = crystalControl;
        this.crystal = crystal;
    }

    public async Task RunAsync(CrystalTestOptions options, string[] args)
    {
        var mono = new Mono();

        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Start: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        var data = this.crystal.Data;
        Console.WriteLine($"Load: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (var i = 0; i < 100_000; i++)
        {
            using (var w = data.TryLock(i, TryLockMode.GetOrCreate))
            {
                if (w is not null)
                {
                    w.Commit();
                }
            }
        }

        Console.WriteLine($"GetOrCreate: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        var bin = TinyhandSerializer.SerializeObject(this.crystal.Data);
        Console.WriteLine($"Serialize {(bin.Length / 1_000_000).ToString()} MB, {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await this.crystal.Save(UnloadMode.ForceUnload);
        Console.WriteLine($"Save {sw.ElapsedMilliseconds} ms");
    }

    public CrystalControl CrystalControl { get; set; }

    private ICrystal<TestClass.GoshujinClass> crystal;
}

public record CrystalTestOptions
{
    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
