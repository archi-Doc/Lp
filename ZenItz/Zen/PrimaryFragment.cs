// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class PrimaryFragment : Fragment
{
    public PrimaryFragment(Flake flake)
        : base(flake)
    {
    }

    public void Set(ReadOnlySpan<byte> data, bool loading)
    {// lock (Flake.syncObject)
        if (data.SequenceEqual(this.MemoryOwner.Memory.Span))
        {// Identical
            return;
        }

        this.MemoryOwner.Return();
        this.MemoryOwner = this.Flake.Zen.PrimaryPool.Rent(data.Length).ToMemoryOwner(0, data.Length);
        data.CopyTo(this.MemoryOwner.Memory.Span);

        this.State = loading ? FragmentState.Saved : FragmentState.NotSaved;
        this.UpdateQueue(FragmentOperation.Set);
    }

    public void Clear()
    {
        this.MemoryOwner.Return();
        this.RemoveQueue();
    }

    internal ByteArrayPool.MemoryOwner MemoryOwner;
}
