// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

// [TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
internal partial class Fragment
{// by Yamamoto.
    public enum FragmentState
    {
        NotLoaded, // Not loaded
        NotSaved, // Active and not saved
        Saved, // Active and saved
    }

    internal Fragment()
    {// 1.For serializer 2.New primary
    }

    internal Fragment(Identifier secondaryId)
    {// 1.New secondary, lock (secondaryGoshujin)
        this.State = FragmentState.Saved;
        this.secondaryId = secondaryId;
        this.SnowFlakeId = SnowmanControl.Instance.GetFlakeId();
    }

    internal void Set(Flake primaryObject, ReadOnlySpan<byte> data)
    {// lock (secondaryGoshujin)
        if (this.himo != null && data.SequenceEqual(this.himo.MemoryOwner.Memory.Span))
        {// Identical
            return;
        }

        this.State = FragmentState.Loaded;
        this.himo = primaryObject.Zen.HimoControl.Create(in primaryObject.identifier, in this.secondaryId, data);
    }

    public FragmentState State { get; private set; }

    public Identifier SecondaryId => this.secondaryId;

    // Serialization & Link
    // [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    private Identifier secondaryId;

    private Himo? himo;
}
