using Tinyhand.IO;
using ValueLink;

namespace Sandbox;

[TinyhandObject(Journaling = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
internal partial record StandardData
{
    public StandardData()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    private int id;

    [Key(1)]
    private string name = string.Empty;

    [Key(2)]
    private double age;

    public override string ToString()
        => $"{this.id} {this.name} ({this.age.ToString()})";
}

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
internal partial class CombinedClass
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    public override string ToString()
        => $"{this.Manual1.ToString()} {this.Manual2.ToString()}";
}
