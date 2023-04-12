using CrystalData.Journal;
using Tinyhand.IO;

namespace Sandbox;

[TinyhandObject]
internal partial class CrystalClass
{
    [Key(0)]
    public int Id { get; set; }

    public override string ToString()
        => $"Crystal class {this.Id}";
}

[TinyhandObject]
internal partial class ManualClass
{
    [Key(0)]
    public int Id { get; set; }

    public override string ToString()
        => $"Manual class {this.Id}";
}

[TinyhandObject]
internal partial class CombinedClass
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    public override string ToString()
        => $"{this.Manual1.ToString()} {this.Manual2.ToString()}";
}

internal class TestClass
{
    public TestClass(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, ManualClass manualClass, IBigCrystal<BaseData> crystalData, ExampleData exampleData)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        // this.manualCrystal.ConfigureFiler(new LocalFilerConfiguration("Manual2.data"));

        // this.manualCrystal.Setup();
        this.combinedCrystal = combinedCrystal;
        this.manualClass0 = manualClass;
        this.exampleData = exampleData;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test1");

        await this.crystalizer.PrepareAndLoadAll();
        await this.crystalizer.PrepareJournal();

        /*var config = new LocalDirectoryConfiguration(""); // new S3DirectoryConfiguration("kiokubako", "lp2");
        var s3filer = this.crystalizer.ResolveRawFiler(config);
        await s3filer.PrepareAndCheck(crystalizer, config);
        var result = await s3filer.ListAsync("Example");
        foreach (var x in result)
        {
            await Console.Out.WriteLineAsync(x.ToString());
        }*/

        // this.manualCrystal.Configure(new(Crystalization.Manual, new LocalFilerConfiguration("test")));

        var manualClass = this.manualCrystal.Object;
        manualClass.Id = 1;
        Console.WriteLine(manualClass.ToString());

        var manualCrystal2 = this.crystalizer.CreateCrystal<ManualClass>();
        manualCrystal2.Configure(new(Crystalization.Manual, new LocalFileConfiguration("test2")));
        var manualClass2 = manualCrystal2.Object;
        manualClass2.Id = 2;
        Console.WriteLine(manualClass.ToString());
        Console.WriteLine(manualClass2.ToString());

        var combinedClass = this.combinedCrystal.Object;
        combinedClass.Manual2.Id = 2;
        Console.WriteLine(combinedClass.ToString());

        Console.WriteLine(this.manualClass0.ToString());

        await combinedCrystal.Save();

        var a = this.exampleData.GetOrCreateChild("a");
        a.BlockDatum().Set(new byte[] { 0, 1, 2, });

        var a2 = this.exampleData.TryGetChild("a");
        var b2 = this.exampleData.TryGetChild("b");

        // await manualCrystal.Delete();
        await manualCrystal.Delete();

        Journal();

        // await this.crystalizer.SaveAll();

        void Journal()
        {
            // Record
            /*if (this.crystalizer.TryGetJournal(out TinyhandWriter writer))
            {// SetValue, AddObject, DeleteObject, Check
                this.WriteLocator(ref writer); // Locator
                writer.WriteInt16(21); // Value
            }*/
            if (this.crystalizer.Journal is { } journal)
            {
                journal.GetJournalWriter(JournalRecordType.SetValue, out var writer);
                // this.WriteLocator(ref record); // Locator
                writer.WriteInt16(32); // Value
                journal.AddRecord(in writer);
            }

            // Read
            /*while (journal.Position != journal.End)
            {
                obj.ReadJournal(journal);
            }*/
        }
    }

    private Crystalizer crystalizer;
    private ManualClass manualClass0;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private ExampleData exampleData;
}
