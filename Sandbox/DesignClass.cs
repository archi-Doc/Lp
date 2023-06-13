using ValueLink;

namespace Sandbox;

public enum LockState
{
    NoLock,
    Share,
    Exclusive,
}

public enum AccessMode
{
    Read,
    ReadWrite,
}

[TinyhandObject]
[ValueLinkObject(Lock = true)]
internal partial class DesignClass
{
    [Key(0)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    private int id;

    [Key(1)]
    private string name = string.Empty;

    [IgnoreMember]
    private LockState lockState;

    internal class Maid
    {
        public Maid(MaidContext context, DesignClass @object)
        {
            this.Context = context;
            this.Object = @object;
        }

        public readonly MaidContext Context;
        public readonly DesignClass? Object;

        public int Id
        {
            get => this.id;
            set
            {
                if (value != this.id)
                {
                    this.id = value;
                    this.__gen_cl_identifier__002?.IdChain.Add(this.id, this);
                }
            }
        }

        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
            }
        }
    }

    internal class Derived : DesignClass
    {
        public Derived()
        {
        }

        public int Id
        {
            get => this.id;
            set
            {
                if (value != this.id)
                {
                    this.id = value;
                    this.__gen_cl_identifier__002?.IdChain.Add(this.id, this);
                }
            }
        }

        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
            }
        }
    }


}

public class MaidContext
{
}

public class Design
{
    internal DesignClass.Maid GetById(int id, AccessMode mode, TimeSpan timeout)
    {
        var goshujin = new DesignClass.GoshujinClass();
        using (goshujin.LockObject.Lock())
        {
            if (goshujin.IdChain.TryGetValue(id, out var obj))
            {
                return new(new MaidContext(), obj);
            }

            return new(new MaidContext(), obj);
        }
    }

    public void Test()
    {

    }
}
