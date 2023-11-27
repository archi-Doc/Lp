// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class FlexArray<T>
{
    public FlexArray()
    {
        this.array = [];
    }

    private T[] array;
    private int startPosition;

    public bool Resize(int size)
    {
        return true;
    }

    public bool Set(int index, T value)
    {
        index -= this.startPosition;
        if (index < 0 || index >= this.array.Length)
        {
            return false;
        }

        this.array[index] = value;
        return true;
    }

    public T? Get(int index)
    {
        index -= this.startPosition;
        if (index < 0 || index >= this.array.Length)
        {
            return default;
        }

        return this.array[index];
    }
}
