// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData;

/// <summary>
/// A example data class.<br/>
/// This is the idea that each data are arranged in the ordered structure and constitute a single crystal.
/// </summary>
[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class ExampleData : BaseData
{
    public ExampleData(IBigCrystal crystal, BaseData? parent, string name)
        : base(crystal, parent)
    {
        this.name = name;
    }

    public ExampleData()
    {
    }

    [Key(4)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    private string name = string.Empty;

    [Key(5)]
    private GoshujinClass? children;

    [Key(6)]
    [Link(Type = ChainType.Ordered)]
    private int age;

    #region Child

    public ExampleData GetOrCreateChild(string name)
    {
        ExampleData? data;
        using (this.semaphore.Lock())
        {
            this.children ??= new();
            if (!this.children.NameChain.TryGetValue(name, out data))
            {
                data = new ExampleData(this.BigCrystal, this, name);
                this.children.Add(data);
            }
        }

        return data;
    }

    public ExampleData? TryGetChild(string name)
    {
        ExampleData? data;
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return null;
            }

            this.children.NameChain.TryGetValue(name, out data);
            return data;
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

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        if (this.children == null)
        {
            yield break;
        }

        foreach (var x in this.children)
        {
            yield return x;
        }
    }

    protected override void DeleteInternal()
    {
        this.children = null;
        this.Goshujin = null;
    }
}
