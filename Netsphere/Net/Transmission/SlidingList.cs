// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere;

public class SlidingList<T>
    where T : class
{
    private const int PositionMask = 0x7FFFFFFF;

    public SlidingList(int size)
    {
        this.items = new T?[size];
    }

    #region FieldAndProperty

    private T?[] items;
    private int headIndex; // The head index in items.
    private int headSize; // The number of items in use, beginning from the head index.
    private int itemsPosition; // Position of the first element in items.

    public int StartPosition => PositionMask & (this.itemsPosition + this.headIndex);

    public int EndPosition => PositionMask & (this.itemsPosition + this.headIndex + this.headSize);

    public bool CanAdd => this.headSize < this.items.Length;

    #endregion

    public bool Resize(int size)
    {
        if (this.items.Length == size)
        {// Identical
            return true;
        }
        else if (this.items.Length < size)
        {// this.items.Length < size
            Array.Resize(ref this.items, size);
            return true;
        }
        else
        {// this.items.Length > size
            this.TrySlide();
            if (this.items[size] is null)
            {
                Array.Resize(ref this.items, size);
                return true;
            }
            else
            {// Cannot resize
                return false;
            }
        }
    }

    public int TrySlide()
    {
        var count = 0;
        for (var i = this.headIndex; i < this.headIndex + this.headSize; i++)
        {
            if (i < this.items.Length)
            {
                if (this.items[i] is null)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            else
            {
                if (this.items[i - this.items.Length] is null)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
        }

        this.headIndex = this.headIndex + count;
        if (this.headIndex >= this.items.Length)
        {
            this.headIndex -= this.items.Length;
            this.itemsPosition += this.items.Length;
        }

        this.headSize -= count;

        return count;
    }

    /*public T? this[int position]
    {
        get => this.Get(position);
        set => this.TrySet(position, value);
    }

    public bool TrySet(int position, T? value)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return false;
        }

        index -= this.offset;
        if (index < 0 || index >= this.items.Length)
        {
            return false;
        }

        this.items[index] = value;
        return true;
    }*/

    public int TryAdd(T value)
    {
        if (!this.CanAdd)
        {
            return -1;
        }

        var index = this.headIndex + this.headSize;
        if (index >= this.items.Length)
        {
            index -= this.items.Length;
        }

        this.headSize++;
        this.items[index] = value;
        return this.IndexToPosition(index);
    }

    public bool TryRemove(int position)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return false;
        }

        this.items[index] = default;
        if (index == this.headIndex)
        {
            this.TrySlide();
        }

        return true;
    }

    public T? TryGet(int position)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return default;
        }

        return this.items[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexToPosition(int index)
    {
        if (index >= this.headIndex)
        {
            return PositionMask & (this.itemsPosition + index);
        }
        else
        {
            return PositionMask & (this.itemsPosition + this.items.Length + index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PositionToIndex(int position)
    {
        var start = this.StartPosition;
        var end = this.EndPosition;
        if (start < end)
        {
            if (start <= position && position < end)
            {
                var index = position - start;
                return index < this.items.Length ? index : index - this.items.Length;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (start <= position)
            {
                var index = position - start;
                return index < this.items.Length ? index : index - this.items.Length;
            }
            else if (position < end)
            {
                return position < this.items.Length ? position : position - this.items.Length;
            }
            else
            {
                return -1;
            }
        }
    }
}
