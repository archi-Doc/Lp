using Tinyhand.IO;
using ValueLink;

namespace Sandbox;

[TinyhandObject(Journaling = true, LockObject = "syncObject")]
[ValueLinkObject]
internal partial class ValueClass // : ITinyhandCustomJournal
{
    [Key(0, AddProperty = "Id")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [Link(Type = ChainType.Ordered, AddValue = false)]
    private string name = string.Empty;

    [Key(2, AddProperty = "Age")]
    private double age;

    [Key(3, AddProperty = "Ttl")]
    [Link(Type = ChainType.Ordered, AddValue = false)]
    private int ttl;

    [IgnoreMember]
    private object syncObject = new();

    public override string ToString()
        => $"Value class {this.id}: {this.name}";

    /*void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
        writer.Write_Locator();
        writer.Write(this.id);
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        var record = reader.Read_Record();
        if (record == JournalRecord.Locator)
        {
            var id = reader.ReadInt32();
            return true;
        }

        return false;
    }*/
}

[TinyhandObject(Journaling = true)]
internal partial class ManualClass
{
    [Key(0, AddProperty = "Id")]
    private int id;

    public override string ToString()
        => $"Manual class {this.id}";
}

[TinyhandObject(Journaling = true)]
internal partial class CombinedClass : ITinyhandCustomJournal
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    public void WriteRecord()
    {
        if (((IJournalObject)this).TryGetJournalWriter(out var journal, out var writer))
        {
            writer.Write_Key();
            writer.Write(1);
            writer.Write_Value();
            TinyhandSerializer.SerializeObject(ref writer, this.Manual2);
            journal.AddJournal(writer);
        }
    }

    public override string ToString()
        => $"{this.Manual1.ToString()} {this.Manual2.ToString()}";

    public void WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    public bool ReadCustomRecord(ref TinyhandReader reader)
    {
        var record = reader.Read_Record();
        if (record == JournalRecord.Key)
        {
            if (reader.ReadInt32() == 1)
            {
                reader.Read_Value();
                this.Manual2 = TinyhandSerializer.DeserializeObject<ManualClass>(ref reader, TinyhandSerializerOptions.Standard)!;
                return true;
            }
        }
        else if (record == JournalRecord.Locator)
        {// tempcode
            var key = reader.ReadInt32();
            var journal = key switch
            {
                0 => this.Manual1 as IJournalObject,
                1 => this.Manual2 as IJournalObject,
                _ => default,
            };

            journal?.ReadRecord(ref reader);

        }

        return false;
    }
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
