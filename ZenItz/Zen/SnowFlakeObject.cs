// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal class SnowFlakeObject : SnowObject
{
    public SnowFlakeObject(Flake flake, SnowObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
        this.fragment = new(flake.Zen);
    }

    public void SetSpan(ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        this.UpdateQueue(SnowObjectOperation.Set, this.fragment.SetSpan(data));
    }

    public void SetObject(object obj)
    {// lock (Flake.syncObject)
        this.UpdateQueue(SnowObjectOperation.Set, this.fragment.SetObject(obj));
    }

    public void SetMemoryOwner(ByteArrayPool.MemoryOwner dataToBeMoved)
    {// lock (Flake.syncObject)
        this.UpdateQueue(SnowObjectOperation.Set, this.fragment.SetMemoryOwner(dataToBeMoved));
    }

    public bool TryGetSpan(out ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        return this.fragment.TryGetSpan(out data);
    }

    public bool TryGetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {// lock (Flake.syncObject)
        return this.fragment.TryGetMemoryOwner(out memoryOwner);
    }

    public bool TryGetObject([MaybeNullWhen(false)] out object? obj)
    {// lock (Flake.syncObject)
        return this.fragment.TryGetObject(out obj);
    }

    public void Unload()
    {// lock (Flake.syncObject)
        this.RemoveQueue(this.fragment.Clear());
    }

    internal override void Save(bool unload)
    {// lock (this.SnowObjectGoshujin.Goshujin)
        if (this.fragment.TryGetMemoryOwner(out var memoryOwner))
        {
            this.Flake.Zen.SnowmanControl.Save(ref this.Flake.flakeSnowId, ref this.Flake.flakeSnowSegment, memoryOwner);
        }

        if (unload)
        {// Unload
            this.Unload();
        }
    }

    private Fragment fragment;
}
