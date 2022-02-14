// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class SnowFlakeObject : SnowObject
{
    public SnowFlakeObject(Flake flake, SnowObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
    }

    public void Set(ReadOnlySpan<byte> data, bool loading)
    {// lock (Flake.syncObject)
        if (data.SequenceEqual(this.MemoryOwner.Memory.Span))
        {// Identical
            return;
        }

        var diff = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner.Return();

        diff += data.Length;
        this.MemoryOwner = this.SnowObjectGoshujin.Pool.Rent(data.Length).ToMemoryOwner(0, data.Length);
        data.CopyTo(this.MemoryOwner.Memory.Span);

        this.State = loading ? SnowObjectState.Saved : SnowObjectState.NotSaved;
        this.UpdateQueue(SnowObjectOperation.Set, diff);
    }

    public void Clear()
    {
        var diff = -this.MemoryOwner.Memory.Length;
        this.MemoryOwner.Return();
        this.RemoveQueue(diff);
    }

    internal override void Save(bool unload)
    {// lock (this.SnowObjectGoshujin.Goshujin)
        if (this.State == SnowObjectState.NotSaved)
        {
            this.Flake.Zen.SnowmanControl.Save(ref this.Flake.flakeSnowId, ref this.Flake.flakeSnowSegment, this.MemoryOwner.IncrementAndShareReadOnly());
        }

        if (unload)
        {
            this.Clear();
        }
    }

    internal ByteArrayPool.MemoryOwner MemoryOwner;
}
