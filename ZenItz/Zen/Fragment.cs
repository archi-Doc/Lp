// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class Fragment
{
    public Fragment(Zen zen, Identifier identifier)
    {
        this.Zen = zen;
        this.Identifier = identifier;
    }

    public int SetObject(object? obj)
    {
        if (obj == this.Object)
        {// Identical
            return 0;
        }

        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwner = this.memoryOwner.Return();
        this.memoryOwnerAvailable = false;

        this.Object = obj;
        return memoryDifference;
    }

    public int SetSpan(ReadOnlySpan<byte> span)
    {
        if (this.memoryOwnerAvailable &&
            span.SequenceEqual(this.memoryOwner.Memory.Span))
        {// Identical
            return 0;
        }

        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwner = this.memoryOwner.Return();

        memoryDifference += span.Length;
        this.Object = null;
        this.memoryOwner = this.Zen.FragmentPool.Rent(span.Length).ToMemoryOwner(0, span.Length);
        span.CopyTo(this.memoryOwner.Memory.Span);
        return memoryDifference;
    }

    public ReadOnlySpan<byte> GetSpan()
    {
        if (this.memoryOwnerAvailable)
        {
            return this.memoryOwner.Memory.Span;
        }
        else if (this.Object != null)
        {
            this.Zen.ObjectToMemoryOwner(this.Object, out this.memoryOwner);
            this.memoryOwnerAvailable = true;
            return this.memoryOwner.Memory.Span;
        }
        else
        {
            return ReadOnlySpan<byte>.Empty;
        }
    }

    public int Clear()
    {
        this.Object = null;
        var memoryDifference = -this.memoryOwner.Memory.Length;
        this.memoryOwner = this.memoryOwner.Return();
        this.memoryOwnerAvailable = false;
        return memoryDifference;
    }

    public Zen Zen { get; }

    [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    public Identifier Identifier { get; private set; }

    public object? Object { get; private set; }

    private bool memoryOwnerAvailable;

    private ByteArrayPool.MemoryOwner memoryOwner;
}
