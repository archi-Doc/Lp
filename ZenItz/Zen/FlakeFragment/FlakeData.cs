// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class FlakeData
{
    public FlakeData(Zen zen)
    {
        this.Zen = zen;
    }

    public (bool Changed, int MemoryDifference) SetSpan(ReadOnlySpan<byte> span)
    {
        if (this.MemoryOwnerAvailable &&
            span.SequenceEqual(this.MemoryOwner.Memory.Span))
        {// Identical
            return (false, 0);
        }

        var memoryDifference = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();

        memoryDifference += span.Length;
        this.Object = null;
        var owner = this.Zen.FlakeFragmentPool.Rent(span.Length);
        this.MemoryOwner = owner.ToReadOnlyMemoryOwner(0, span.Length);
        this.MemoryOwnerAvailable = true;
        span.CopyTo(owner.ByteArray.AsSpan());
        return (true, memoryDifference);
    }

    public (bool Changed, int MemoryDifference) SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
    {
        if (this.MemoryOwnerAvailable &&
            dataToBeMoved.Memory.Span.SequenceEqual(this.MemoryOwner.Memory.Span))
        {// Identical
            return (false, 0);
        }

        var memoryDifference = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();

        memoryDifference += dataToBeMoved.Memory.Length;
        this.Object = null;
        this.MemoryOwner = dataToBeMoved;
        this.MemoryOwnerAvailable = true;
        return (true, memoryDifference);
    }

    public (bool Changed, int MemoryDifference) SetMemoryOwner(ByteArrayPool.MemoryOwner dataToBeMoved)
        => this.SetMemoryOwner(dataToBeMoved.AsReadOnly());

    public (bool Changed, int MemoryDifference) SetObject(object? obj)
    {
        /* Skip (may be an updated object of the same instance)
        if (obj == this.Object)
        {// Identical
            return 0;
        }*/

        var memoryDifference = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();
        this.MemoryOwnerAvailable = false;

        this.Object = obj;
        return (true, memoryDifference);
    }

    public bool TryGetSpan(out ReadOnlySpan<byte> data)
    {
        if (this.MemoryOwnerAvailable)
        {
            data = this.MemoryOwner.Memory.Span;
            return true;
        }
        else if (this.Object != null)
        {
            this.Zen.ObjectToMemoryOwner(this.Object, out var m);
            this.MemoryOwner = m.AsReadOnly();
            this.MemoryOwnerAvailable = true;
            data = this.MemoryOwner.Memory.Span;
            return true;
        }
        else
        {
            data = default;
            return false;
        }
    }

    public bool TryGetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwmer)
    {
        if (this.MemoryOwnerAvailable)
        {
            memoryOwmer = this.MemoryOwner.IncrementAndShare();
            return true;
        }
        else if (this.Object != null)
        {
            this.Zen.ObjectToMemoryOwner(this.Object, out var m);
            this.MemoryOwner = m.AsReadOnly();
            this.MemoryOwnerAvailable = true;
            memoryOwmer = this.MemoryOwner.IncrementAndShare();
            return true;
        }
        else
        {
            memoryOwmer = default;
            return false;
        }
    }

    public bool TryGetObject([MaybeNullWhen(false)]out object? obj)
    {
        return (obj = this.Object) != null ? true : false;
    }

    public int Clear()
    {
        this.Object = null;
        var memoryDifference = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();
        this.MemoryOwnerAvailable = false;
        return memoryDifference;
    }

    public Zen Zen { get; }

    public object? Object { get; private set; }

    public bool MemoryOwnerAvailable { get; private set; }

    internal ByteArrayPool.ReadOnlyMemoryOwner MemoryOwner;
}
