// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class SnowFragmentObject : SnowObject
{
    public SnowFragmentObject(Flake flake, SnowObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
    }

    public ZenResult SetSpan(Identifier fragmentId, ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        Fragment2? fragment2;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragment2))
        {
            fragment2 = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragment2);
        }

        this.UpdateQueue(SnowObjectOperation.Set, fragment2.SetSpan(data));
        return ZenResult.Success;
    }

    public ZenResult SetObject(Identifier fragmentId, object obj)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        Fragment2? fragment2;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragment2))
        {
            fragment2 = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragment2);
        }

        this.UpdateQueue(SnowObjectOperation.Set, fragment2.SetObject(obj));
        return ZenResult.Success;
    }

    public ZenResult SetMemoryOwner(Identifier fragmentId, ByteArrayPool.MemoryOwner dataToBeMoved)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        Fragment2? fragment2;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragment2))
        {
            fragment2 = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragment2);
        }

        this.UpdateQueue(SnowObjectOperation.Set, fragment2.SetMemoryOwner(dataToBeMoved));
        return ZenResult.Success;
    }

    public bool TryGetSpan(Identifier fragmentId, out ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment2))
        {// Fount
            return fragment2.TryGetSpan(out data);
        }
        else
        {
            data = default;
            return false;
        }
    }

    public bool TryGetMemoryOwner(Identifier fragmentId, out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment2))
        {// Fount
            return fragment2.TryGetMemoryOwner(out memoryOwner);
        }
        else
        {
            memoryOwner = default;
            return false;
        }
    }

    public bool TryGetObject(Identifier fragmentId, [MaybeNullWhen(false)] out object? obj)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment2))
        {// Fount
            return fragment2.TryGetObject(out obj);
        }
        else
        {
            obj = default;
            return false;
        }
    }

    public void Unload()
    {// lock (Flake.syncObject)
        int memoryDifference = 0;
        if (this.fragments != null)
        {
            foreach (var x in this.fragments.IdChain)
            {
                memoryDifference += x.Clear();
            }

            this.fragments.Clear();
            this.fragments = null;
        }

        this.RemoveQueue(memoryDifference);
    }

    internal override void Save(bool unload)
    {// lock (Flake.syncObject) -> lock (this.SnowObjectGoshujin.Goshujin)
        if (this.fragments != null)
        {
            var writer = default(Tinyhand.IO.TinyhandWriter);
            var options = TinyhandSerializerOptions.Standard;
            foreach (var x in this.fragments.IdChain)
            {
                if (x.TryGetSpan(out var span))
                {
                    x.Identifier.Serialize(ref writer, options);
                    writer.WriteSpan(span);
                }
            }

            var memoryOwner = new ByteArrayPool.ReadOnlyMemoryOwner(writer.FlushAndGetArray());
            this.Flake.Zen.SnowmanControl.Save(ref this.Flake.fragmentSnowId, ref this.Flake.fragmentSnowSegment, memoryOwner);
        }

        if (unload)
        {// Unload
            this.Unload();
        }
    }

    private Fragment2.GoshujinClass? fragments; // by Yamamoto.
}
