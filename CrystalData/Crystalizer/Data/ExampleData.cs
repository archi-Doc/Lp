// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using System.Diagnostics.CodeAnalysis;

namespace CrystalData;

/// <summary>
/// A example data class.<br/>
/// This is the idea that each data are arranged in the ordered structure and constitute a single crystal.
/// </summary>
[TinyhandObject(Tree = true, ExplicitKeyOnly = true)]
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

    public ExampleData? TryGetChild(string name)
        => this.children?.TryGet(name);

    public WriterClass? TryLockChild(string name, TryLockMode mode)
    {
        if (mode == TryLockMode.Get)
        {
            return this.children?.TryLock(name, mode);
        }

        if (this.children is null)
        {
            this.EnsureChildren();
        }

        return this.children.TryLock(name, mode);
    }

    public bool DeleteChild(string name)
    {
        if (this.children is not null)
        {
            using (var w = this.children.TryLock(name))
            {
                if (w is not null)
                {
                    w.Goshujin = null;
                    w.Commit()?.DeleteActual();
                    return true;
                }
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

    [MemberNotNull(nameof(children))]
    private void EnsureChildren()
    {
        if (this.Parent is ExampleData parent &&
               parent.Children is { } c)
        {
            using (var w = c.TryLock(this.Name, TryLockMode.Get))
            {
                if (w is not null)
                {
                    if (w.Children is not null)
                    {
                        this.children = w.Children;
                    }
                    else
                    {
                        w.Children ??= new();
                        this.children = w.Commit()?.children;
                    }
                }
            }
        }

        this.children ??= new();
    }
}
