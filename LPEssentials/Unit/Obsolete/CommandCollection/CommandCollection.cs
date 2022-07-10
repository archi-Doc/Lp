// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Arc.Unit.Obsolete;

public class CommandCollection : ICommandCollection
{
    private readonly List<CommandDescriptor> descriptors = new();

    public void AddCommand(Type commandType) => this.descriptors.Add(new(commandType));

    /// <inheritdoc />
    public int Count => this.descriptors.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public CommandDescriptor this[int index]
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
    public bool Contains(CommandDescriptor item)
    {
        return this.descriptors.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(CommandDescriptor[] array, int arrayIndex)
    {
        this.descriptors.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(CommandDescriptor item)
    {
        return this.descriptors.Remove(item);
    }

    /// <inheritdoc />
    public IEnumerator<CommandDescriptor> GetEnumerator()
    {
        return this.descriptors.GetEnumerator();
    }

    void ICollection<CommandDescriptor>.Add(CommandDescriptor item)
    {
        this.descriptors.Add(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <inheritdoc />
    public int IndexOf(CommandDescriptor item)
    {
        return this.descriptors.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, CommandDescriptor item)
    {
        this.descriptors.Insert(index, item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        this.descriptors.RemoveAt(index);
    }
}
