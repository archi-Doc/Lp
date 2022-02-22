// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

internal partial class FragmentObject : FlakeObjectBase
{
    public FragmentObject(Flake flake, FlakeObjectGoshujin goshujin)
        : base(flake, goshujin)
    {
    }

    public ZenResult SetSpan(Identifier fragmentId, ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        FragmentData? fragmentData;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
        {
            fragmentData = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragmentData);
        }

        this.UpdateQueue(FlakeObjectOperation.Set, fragmentData.SetSpan(data));
        return ZenResult.Success;
    }

    public ZenResult SetObject(Identifier fragmentId, object obj)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        FragmentData? fragmentData;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
        {
            fragmentData = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragmentData);
        }

        this.UpdateQueue(FlakeObjectOperation.Set, fragmentData.SetObject(obj));
        return ZenResult.Success;
    }

    public ZenResult SetMemoryOwner(Identifier fragmentId, ByteArrayPool.MemoryOwner dataToBeMoved)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        FragmentData? fragmentData;
        if (this.fragments.IdChain.Count >= Zen.MaxFragmentCount)
        {
            return ZenResult.OverNumberLimit;
        }
        else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
        {
            fragmentData = new(this.Flake.Zen, fragmentId);
            this.fragments.Add(fragmentData);
        }

        this.UpdateQueue(FlakeObjectOperation.Set, fragmentData.SetMemoryOwner(dataToBeMoved));
        return ZenResult.Success;
    }

    public bool TryGetSpan(Identifier fragmentId, out ReadOnlySpan<byte> data)
    {// lock (Flake.syncObject)
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment))
        {// Fount
            return fragment.TryGetSpan(out data);
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
        {
            this.fragments = this.PrepareFragments();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
        {// Fount
            return fragmentData.TryGetMemoryOwner(out memoryOwner);
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
        {
            this.fragments = this.PrepareFragments();
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
        {// Fount
            return fragmentData.TryGetObject(out obj);
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
    {// lock (Flake.syncObject) -> lock (this.FlakeObjectGoshujin.Goshujin)
        if (this.fragments != null)
        {
            var writer = default(Tinyhand.IO.TinyhandWriter);
            var options = TinyhandSerializerOptions.Standard;
            foreach (var x in this.fragments.IdChain)
            {
                if (x.TryGetSpan(out var span))
                {
                    x.Identifier.Serialize(ref writer, options);
                    writer.Write(span);
                }
            }

            var memoryOwner = new ByteArrayPool.ReadOnlyMemoryOwner(writer.FlushAndGetArray());
            this.Flake.Zen.IO.Save(ref this.Flake.fragmentFile, memoryOwner);
        }

        if (unload)
        {// Unload
            this.Unload();
        }
    }

    internal bool Load(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        if (this.fragments != null)
        {
            return true;
        }

        this.fragments = new();
        var reader = new Tinyhand.IO.TinyhandReader(memoryOwner.Memory);
        var options = TinyhandSerializerOptions.Standard;
        try
        {
            while (!reader.End)
            {
                var identifier = default(Identifier);
                identifier.Deserialize(ref reader, options);
                var byteArray = reader.ReadBytesToArray();

                var fragment = new FragmentData(this.Flake.Zen, identifier);
                fragment.SetSpan(byteArray);
                this.fragments.Add(fragment);
            }
        }
        finally
        {
        }

        return true;
    }

    private FragmentData.GoshujinClass PrepareFragments()
    {// lock (Flake.syncObject)
        if (this.fragments != null)
        {
            return this.fragments;
        }
        else if (ZenFile.IsValidFile(this.Flake.fragmentFile))
        {
            var result = this.Flake.Zen.IO.Load(this.Flake.fragmentFile).Result;
            if (result.IsSuccess)
            {
                if (this.Load(result.Data))
                {
                    return this.fragments!;
                }
            }
        }

        return new();
    }

    private FragmentData.GoshujinClass? fragments; // by Yamamoto.
}
