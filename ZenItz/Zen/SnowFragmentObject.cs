// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class SnowFragmentObject : SnowObject
{
    public SnowFragmentObject(SnowObjectGoshujin goshujin)
        : base(goshujin)
    {
    }

    public ZenResult Set(Identifier fragmentId, ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        if (this.fragments.Count >= Zen.MaxSecondaryFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (this.fragments.TryGetValue(fragmentId, out var memoryOwner))
        {
            if (data.SequenceEqual(memoryOwner.Memory.Span))
            {// Identical
                return ZenResult.Success;
            }

            memoryOwner.Return();
        }

        var memoryOwner2 = this.SnowObjectGoshujin.Zen.SecondaryPool.Rent(data.Length).ToMemoryOwner(0, data.Length);
        data.CopyTo(memoryOwner2.Memory.Span);
        this.fragments[fragmentId] = memoryOwner2;

        this.UpdateQueue(SnowObjectOperation.Set);
        return ZenResult.Success;
    }

    public void Clear()
    {
        if (this.fragments != null)
        {
            foreach (var x in this.fragments.Values)
            {
                x.Return();
            }

            this.fragments = null;
        }

        this.RemoveQueue();
    }

    private Dictionary<Identifier, ByteArrayPool.MemoryOwner>? fragments; // by Yamamoto.
}
