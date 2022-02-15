// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class SnowFragmentObject : SnowObject
{
    public SnowFragmentObject(Flake flake, SnowObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
    }

    public ZenResult Set(Identifier fragmentId, ReadOnlySpan<byte> data, bool loading)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {// tempcode, load
            this.fragments = new();
        }

        int memoryDifference = 0;
        Fragment? fragment;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragment))
        {
            fragment = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragment);
        }

        memoryDifference = fragment.SetSpan(data);
        this.UpdateQueue(SnowObjectOperation.Set, memoryDifference);
        return ZenResult.Success;
    }

    public void Clear()
    {
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
    {
        throw new NotImplementedException();
    }

    private Fragment.GoshujinClass? fragments; // by Yamamoto.
}
