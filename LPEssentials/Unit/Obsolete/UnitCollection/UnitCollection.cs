// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace LP.Unit.Obsolete;

public class UnitCollection : IUnitCollection
{
    private readonly List<UnitDescriptor> descriptors = new();

    public void AddUnit<TUnit>(bool createInstance)
        where TUnit : UnitBase => this.descriptors.Add(new(typeof(TUnit), createInstance));

    /// <inheritdoc />
    public int Count => this.descriptors.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public UnitDescriptor this[int index]
    {
        get
        {
            return this.descriptors[index];
        }

        set
        {
            this.descriptors[index] = value;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        this.descriptors.Clear();
    }

    /// <inheritdoc />
    public bool Contains(UnitDescriptor item)
    {
        return this.descriptors.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(UnitDescriptor[] array, int arrayIndex)
    {
        this.descriptors.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(UnitDescriptor item)
    {
        return this.descriptors.Remove(item);
    }

    /// <inheritdoc />
    public IEnumerator<UnitDescriptor> GetEnumerator()
    {
        return this.descriptors.GetEnumerator();
    }

    void ICollection<UnitDescriptor>.Add(UnitDescriptor item)
    {
        this.descriptors.Add(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <inheritdoc />
    public int IndexOf(UnitDescriptor item)
    {
        return this.descriptors.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, UnitDescriptor item)
    {
        this.descriptors.Insert(index, item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        this.descriptors.RemoveAt(index);
    }
}
