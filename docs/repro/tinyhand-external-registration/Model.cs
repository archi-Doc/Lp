using Tinyhand;
#if CASE_Owner
using ValueLink;
#endif

namespace ExternalRegistrationRepro;

#if CASE_Owner
[TinyhandObject]
[ValueLinkObject]
public partial class Model
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { get; set; }

    [TinyhandObject(External = true)]
    public partial class GoshujinClass { }
}
#elif CASE_Normal
[TinyhandObject]
public partial class Model { }
#elif CASE_Plain
public partial class Model { }
#else
// No ValueLink dependency, no serializer invocation: this declaration alone reproduces CS0311.
[TinyhandObject(External = true)]
public partial class Model { }
#endif
