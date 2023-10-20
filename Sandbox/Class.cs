using LP;
using Tinyhand.IO;
using ValueLink;

namespace Sandbox;

[TinyhandObject(ReservedKeys = 1)]
// [ValueLinkObject]
public abstract partial class TtlBase
{
    public TtlBase()
    {
        var g = new TtlData.GoshujinClass();
    }

    [Key(0)]
    public long ExpirationMics { get; set; }
}

[TinyhandObject]
[ValueLinkObject]
public partial class TtlData : TtlBase
{
    public TtlData()
    {
    }

    [Key(2)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public string Name { get; set; } = string.Empty;

}

[TinyhandObject(Tree = true)]
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

[TinyhandObject(Tree = true)]
internal partial class ManualClass
{
    [Key(0, AddProperty = "Id")]
    private int id;

    public override string ToString()
        => $"Manual class {this.id}";
}

[TinyhandObject(Tree = true)]
internal partial class CombinedClass
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    public override string ToString()
        => $"{this.Manual1.ToString()} {this.Manual2.ToString()}";
}
