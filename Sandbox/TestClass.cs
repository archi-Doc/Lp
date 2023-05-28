using System.Threading.Tasks.Sources;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace Sandbox;

[TinyhandObject(Journaling = true)]
[ValueLinkObject]
internal partial class ValueClass : ITinyhandCustomJournal
{
    [Key(0, AddProperty = "Id")]
    [Link(Type = ChainType.Unordered, Primary = true, Accessibility = ValueLinkAccessibility.Public)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
    private string name = string.Empty;

    bool ReadRecordGoshujin(ref TinyhandReader reader)
    {
        var record = reader.Read_Record();
        if (record == JournalRecord.Locator)
        {
            var id = reader.ReadInt32();
            if (this.Goshujin?.IdChain.FindFirst(id) is ITinyhandJournal journalObject)
            {
                return journalObject.ReadRecord(ref reader);
            }
        }
        /*else if (record == JournalRecord.Key)
        {// Tinyhand
            var options = TinyhandSerializerOptions.Standard;
            var key = reader.ReadInt32();
            if (key == 0)
            {
                reader.Read_Value();
                this.IdValue = reader.ReadInt32();
                return true;
            }
            else if (key == 1)
            {
                reader.Read_Value();
                this.name = reader.ReadString() ?? string.Empty;
                return true;
            }
            else
            {
                reader.Skip();
            }
        }*/
        else if (record == JournalRecord.Add)
        {// ValueLink
            if (reader.TryReadBytes(out var span))
            {
                try
                {
                    var obj = TinyhandSerializer.DeserializeObject<ValueClass>(span);
                    if (obj is not null)
                    {
                        obj.Goshujin = this.Goshujin; // this
                        return true;
                    }
                }
                catch { }
            }
        }
        else if (record == JournalRecord.Remove)
        {// ValueLink
            // TinyhandSerializer.DeserializeObject<int>(ref reader, );
            var id = reader.ReadInt32();
            if (this.Goshujin?.IdChain.FindFirst(id) is { } obj)
            {
                obj.Goshujin = null;
                return true;
            }
        }
        return false;
    }

    [IgnoreMember]
    public int IdCombined
    {
        get => this.id;
        set
        {
            if (value != this.id)
            {
                // ValueLink
                this.id = value;
                // this.__gen_cl_identifier__002?.IdChain.Add(this.id, this);
                this.Goshujin?.IdChain.Add(this.id, this);

                // Tinyhand
                if (this.Crystal is not null && this.Crystal.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer))
                {
                    if (this is ITinyhandCustomJournal custom)
                    {
                        custom.WriteCustomLocator(ref writer);
                    }

                    writer.Write_Key();
                    writer.Write(0);
                    writer.Write_Value();
                    writer.Write(this.id);
                    this.Crystal.AddJournal(writer);
                }
            }
        }
    }

    [IgnoreMember]
    public GoshujinClass? Goshujin2
    {
        get => this.__gen_cl_identifier__002;
        set
        {
            if (value != this.__gen_cl_identifier__002)
            {
                if (this.__gen_cl_identifier__002 != null)
                {// ValueLink
                    this.__gen_cl_identifier__002.IdChain.Remove(this);
                    this.__gen_cl_identifier__002.NameChain.Remove(this);

                    // ValueLink
                    if (this.Crystal is not null && this.Crystal.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer))
                    {
                        // Remove
                        if (this is ITinyhandCustomJournal custom)
                        {
                            custom.WriteCustomLocator(ref writer);
                        }

                        writer.Write_Remove();
                        writer.Write(this.id);
                        this.Crystal.AddJournal(writer);
                    }
                }

                this.__gen_cl_identifier__002 = value;
                if (value != null)
                {
                    // ValueLink
                    value.IdChain.Add(this.id, this);
                    value.NameChain.Add(this.name, this);

                    // ValueLink
                    if (this.Crystal is not null && this.Crystal.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer))
                    {
                        // Add
                        if (this is ITinyhandCustomJournal custom)
                        {
                            custom.WriteCustomLocator(ref writer);
                        }

                        writer.Write_Add();
                        writer.Write(TinyhandSerializer.SerializeObject(this));
                        this.Crystal.AddJournal(writer);
                    }
                }
            }
        }
    }

    public override string ToString()
        => $"Value class {this.id}: {this.name}";

    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
        writer.Write_Locator();
        writer.Write(this.id);
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        /*var record = reader.Read_Record();
        if (record == JournalRecord.Locator)
        {
            var id = reader.ReadInt32();
            return true;
        }*/

        return false;
    }
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
        if (this.Crystal?.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer) == true)
        {
            writer.Write_Key();
            writer.Write(1);
            writer.Write_Value();
            TinyhandSerializer.SerializeObject(ref writer, this.Manual2);
            this.Crystal.AddJournal(writer);
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
                0 => this.Manual1 as ITinyhandJournal,
                1 => this.Manual2 as ITinyhandJournal,
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
