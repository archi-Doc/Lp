using Tinyhand;
using Tinyhand.IO;

namespace Sandbox;

[TinyhandObject (Journaling = true)]
internal partial class ManualClass
{
    [Key(0, AddProperty = "Id")]
    private int id;

    public override string ToString()
        => $"Manual class {this.id}";
}

[TinyhandObject]
internal partial class CombinedClass : ITinyhandJournal
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    [IgnoreMember]
    public ITinyhandCrystal? Crystal { get; set; }

    [IgnoreMember]
    public uint CurrentPlane { get; set; }

    bool ITinyhandJournal.ReadRecord(ref TinyhandReader reader)
    {
        return false;
    }

    public bool ReadRecord(ref TinyhandReader reader)
    {
        throw new NotImplementedException();
    }

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

        this.combinedCrystal = combinedCrystal;
        this.manualClass0 = manualClass;
        this.exampleData = exampleData;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test1");

        await this.crystalizer.PrepareAndLoadAll();

        var manualClass = this.manualCrystal.Data;
        manualClass.Id = 1;
        Console.WriteLine(manualClass.ToString());

        var manualCrystal2 = this.crystalizer.CreateCrystal<ManualClass>();
        manualCrystal2.Configure(new(SavePolicy.Manual, new LocalFileConfiguration("test2/manual2")));
        var manualClass2 = manualCrystal2.Data;
        manualClass2.Id = 2;
        await manualCrystal2.Save();
        Console.WriteLine(manualClass.ToString());
        Console.WriteLine(manualClass2.ToString());

        var combinedClass = this.combinedCrystal.Data;
        combinedClass.Manual2.Id = 2;
        Console.WriteLine(combinedClass.ToString());

        Console.WriteLine(this.manualClass0.ToString());

        await combinedCrystal.Save();

        var a = this.exampleData.GetOrCreateChild("a");
        a.BlockDatum().Set(new byte[] { 0, 1, 2, });

        var a2 = this.exampleData.TryGetChild("a");
        var b2 = this.exampleData.TryGetChild("b");
    }

    private Crystalizer crystalizer;
    private ManualClass manualClass0;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private ExampleData exampleData;
}
