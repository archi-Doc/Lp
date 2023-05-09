using CrystalData.Journal;
using LP.T3CS;
using Tinyhand.IO;

namespace Sandbox;

[TinyhandObject]
internal partial class CrystalClass : IJournalObject, ICrystalData
{
    private int id;
    private HashSet<string> names = new();

    [Key(0)]
    public int Id
    {
        get => this.id;
        set
        {
            // using (this.semaphore.Lock())
            {
                this.id = value;
                if (this.Crystal?.Crystalizer.Journal is { } journal)
                {
                    journal.GetWriter(JournalType.Record, this.CurrentPlane, out var writer);
                    writer.Write_Key();
                    writer.Write(0);
                    writer.Write_Value();
                    writer.Write(this.id);
                    journal.Add(writer);
                }
            }

            if (this.Crystal?.CrystalConfiguration.SavePolicy == SavePolicy.OnChanged)
            {
                this.Crystal.Crystalizer.AddToSaveQueue(this.Crystal);
            }
        }
    }

    [IgnoreMember]
    public ICrystal? Crystal { get; set; }

    [IgnoreMember]
    public uint CurrentPlane { get; set; }

    public bool AddName(string name)
    {
        var result = this.names.Add(name);

        if (this.Crystal?.Crystalizer.Journal is { } journal)
        {
            journal.GetWriter(JournalType.Record, this.CurrentPlane, out var writer);
            writer.Write_Key();
            writer.Write(1);
            writer.Write_Add();
            writer.Write(name);
            journal.Add(writer);
        }

        return result;
    }

    public override string ToString()
        => $"Crystal class {this.Id}";

    void ICrystalData.ReadRecord(ref TinyhandReader reader)
    {
        // Custom
        var fork = reader.Fork();
        if (this.ReadCustomRecord(ref fork))
        {
            return;
        }

        // Generated
        var dataType = reader.Read_Record();
        if (dataType == JournalRecord.Key)
        {
            var key = reader.ReadInt32();
            if (key == 0)
            {
                reader.Read_Value();
                this.id = reader.ReadInt32();
            }
            else if (key == 1)
            {
            }
        }
    }

    private bool ReadCustomRecord(ref TinyhandReader reader)
    {
        reader.Read_Key();
        var key = reader.ReadInt32();
        if (key == 1)
        {// names
            var record = reader.Read_Record();
            if (record == JournalRecord.Add)
            {
                var name = reader.ReadString();
                if (name != null)
                {
                    this.names.Add(name);
                }

                return true;
            }
        }

        return false;
    }

    void ICrystalData.WriteLocator(ref TinyhandWriter writer)
    {
    }
}

[TinyhandObject]
internal partial class ManualClass : IJournalObject
{
    [KeyAsName]
    public int Id { get; set; }

    public override string ToString()
        => $"Manual class {this.Id}";
}

[TinyhandObject]
internal partial class CombinedClass : IJournalObject
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

        this.combinedCrystal = combinedCrystal;
        this.manualClass0 = manualClass;
        this.exampleData = exampleData;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test1");

        await this.crystalizer.PrepareAndLoadAll();

        var manualClass = this.manualCrystal.Object;
        manualClass.Id = 1;
        Console.WriteLine(manualClass.ToString());

        var manualCrystal2 = this.crystalizer.CreateCrystal<ManualClass>();
        manualCrystal2.Configure(new(SavePolicy.Manual, new LocalFileConfiguration("test2/manual2")));
        var manualClass2 = manualCrystal2.Object;
        manualClass2.Id = 2;
        await manualCrystal2.Save();
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
    }

    private Crystalizer crystalizer;
    private ManualClass manualClass0;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private ExampleData exampleData;
}
