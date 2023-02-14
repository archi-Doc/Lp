// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

internal partial class DualData
{
    public DualData()
    {
    }

    internal (bool Changed, int MemoryDifference) SetSpanInternal(ReadOnlySpan<byte> span)
    {
        if (this.memoryOwnerIsValid && span.SequenceEqual(this.memoryOwner.Memory.Span))
        {// Identical
            return (false, 0);
        }

        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwner = this.memoryOwner.Return();

        memoryDifference += span.Length;
        this.@object = null;
        var owner = FlakeFragmentPool.Rent(span.Length);
        this.memoryOwner = owner.ToReadOnlyMemoryOwner(0, span.Length);
        this.memoryOwnerIsValid = true;
        span.CopyTo(owner.ByteArray.AsSpan());
        return (true, memoryDifference);
    }

    internal (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj)
    {
        if (this.memoryOwnerIsValid && dataToBeMoved.Memory.Span.SequenceEqual(this.memoryOwner.Memory.Span))
        {// Identical
            return (false, 0);
        }

        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwner = this.memoryOwner.Return();

        memoryDifference += dataToBeMoved.Memory.Length;
        this.@object = obj;
        this.memoryOwner = dataToBeMoved;
        this.memoryOwnerIsValid = true;
        return (true, memoryDifference);
    }

    internal ZenResult TryGetObjectInternal<T>(out T? obj)
        where T : ITinyhandSerialize<T>
    {
        if (this.@object is T t)
        {
            obj = t;
            return ZenResult.Success;
        }

        try
        {
            obj = TinyhandSerializer.DeserializeObject<T>(this.memoryOwner.Memory.Span);
            if (obj != null)
            {
                this.@object = obj;
                return ZenResult.Success;
            }
        }
        catch
        {
        }

        obj = default;
        return ZenResult.DeserializeError;
    }

    internal int Clear()
    {
        this.@object = null;
        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwnerIsValid = false;
        this.memoryOwner = this.memoryOwner.Return();
        return memoryDifference;
    }

    internal ReadOnlySpan<byte> Span => this.memoryOwner.Memory.Span;

    internal bool MemoryOwnerIsValid => this.memoryOwnerIsValid;

    internal ByteArrayPool.ReadOnlyMemoryOwner MemoryOwner => this.memoryOwner;

    private object? @object;
    private bool memoryOwnerIsValid = false;
    private ByteArrayPool.ReadOnlyMemoryOwner memoryOwner;
}
