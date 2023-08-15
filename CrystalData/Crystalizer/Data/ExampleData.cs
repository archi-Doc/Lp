// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData;

/// <summary>
/// A example data class.<br/>
/// This is the idea that each data are arranged in the ordered structure and constitute a single crystal.
/// </summary>
[TinyhandObject(Journal = true, ExplicitKeyOnly = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record ExampleData : BaseData
{
    public ExampleData(IBigCrystal crystal, BaseData? parent, string name)
        : base(crystal, parent)
    {
        this.name = name;
    }

    public ExampleData()
    {
    }

    [Key(4, AddProperty = "Name")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    private string name = string.Empty;

    [Key(5, AddProperty = "Children")]
    private GoshujinClass? children;

    [Key(6, AddProperty = "Age")]
    [Link(Type = ChainType.Ordered)]
    private int age;

    #region Children

    public override ExampleData[] GetChildren()
    {
        if (this.children == null)
        {
            return Array.Empty<ExampleData>();
        }
        else
        {
            return this.children.GetArray();
        }
    }

    public bool DeleteChild(string name)
    {
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return false;
            }

            if (this.children.NameChain.TryGetValue(name, out var data))
            {
                data.DeleteActual();
                return true;
            }
        }

        return false;
    }

    #endregion

    protected override void DeleteInternal()
    {
        this.children = null;
        this.Goshujin = null;
    }
}
