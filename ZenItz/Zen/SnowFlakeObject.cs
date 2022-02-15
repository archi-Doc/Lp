// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class SnowFlakeObject : SnowObject
{
    public SnowFlakeObject(Flake flake, SnowObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
        this.fragment = new(flake.Zen, flake.Identifier);
    }

    public void Set(ReadOnlySpan<byte> data, bool loading)
    {// lock (Flake.syncObject)
        if (data.SequenceEqual(this.MemoryOwner.Memory.Span))
        {// Identical
            return;
        }

        var diff = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();

        diff += data.Length;
        this.MemoryOwner = this.SnowObjectGoshujin.Pool.Rent(data.Length).ToMemoryOwner(0, data.Length);
        data.CopyTo(this.MemoryOwner.Memory.Span);

        this.CachedObject = null;
        this.State = SnowObjectState.Memory;
        // this.State = loading ? SnowObjectState.Saved : SnowObjectState.NotSaved;
        this.UpdateQueue(SnowObjectOperation.Set, diff);
    }

    public void Set(object obj)
    {// lock (Flake.syncObject)
        if (obj == this.CachedObject)
        {// Identical
            return;
        }

        var diff = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();

        this.CachedObject = obj;
        this.State = SnowObjectState.Object;
        this.UpdateQueue(SnowObjectOperation.Set, diff);
    }

    public void Unload()
    {// Object, Memory
        this.CachedObject = null;
        var diff = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner = this.MemoryOwner.Return();
        this.RemoveQueue(diff);
    }

    internal override void Save(bool unload)
    {// lock (this.SnowObjectGoshujin.Goshujin)
        if (this.State == SnowObjectState.Object)
        {// Object to Memory
            this.Flake.Zen.ObjectToMemoryOwner(this.CachedObject, out this.MemoryOwner);
            this.CachedObject = null;
            this.State = SnowObjectState.Memory;
        }

        if (this.State == SnowObjectState.Memory)
        {// Memory to File
            this.Flake.Zen.SnowmanControl.Save(ref this.Flake.flakeSnowId, ref this.Flake.flakeSnowSegment, this.MemoryOwner.IncrementAndShareReadOnly());
        }

        if (unload)
        {// Unload
            this.Unload();
        }
    }

    internal ByteArrayPool.MemoryOwner MemoryOwner;
    internal object? CachedObject;
    private Fragment fragment;
}
