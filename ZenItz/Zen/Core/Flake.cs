// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ZenItz;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class Flake
{
    [Link(Primary = true, Name = "RecentGet", Type = ChainType.LinkedList)]
    internal Flake()
    {
    }

    internal Flake(Zen zen, Identifier identifier)
    {
        this.Zen = zen;
        this.identifier = identifier;
    }

    public ZenResult Set(ReadOnlySpan<byte> data)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }
        else if (data.Length > Zen.MaxFlakeSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
            this.flakeObject.SetSpan(data);
        }

        return ZenResult.Success;
    }

    /*internal ZenResult Set(ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }
        else if (dataToBeMoved.Memory.Length > Zen.MaxFlakeSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
            this.flakeObject.SetMemoryOwner(dataToBeMoved);
        }

        return ZenResult.Success;
    }*/

    public ZenResult SetObject(object obj)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
            this.flakeObject.SetObject(obj);
        }

        return ZenResult.Success;
    }

    public ZenResult SetFragment(Identifier fragmentId, ReadOnlySpan<byte> data)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }
        else if (data.Length > Zen.MaxFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
            return this.fragmentObject.SetSpan(fragmentId, data);
        }
    }

    /*internal ZenResult Set(Identifier fragmentId, ByteArrayPool.MemoryOwner data)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }
        else if (data.Memory.Length > Zen.MaxFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
            return this.fragmentObject.SetMemoryOwner(fragmentId, data);
        }
    }*/

    public ZenResult SetFragmentObject(Identifier fragmentId, object obj)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
            return this.fragmentObject.SetObject(fragmentId, obj);
        }
    }

    public async Task<ZenDataResult> Get()
    {
        if (!this.Zen.Started)
        {
            return new(ZenResult.NotStarted);
        }

        ulong file = 0;
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return new(ZenResult.Removed);
            }

            if (this.flakeObject != null && this.flakeObject.TryGetMemoryOwner(out var memoryOwner))
            {// Memory
                this.UpdateGetRecentLink();
                return new(ZenResult.Success, memoryOwner);
            }

            file = this.flakeFile;
        }

        if (ZenFile.IsValidFile(file))
        {
            var result = await this.Zen.IO.Load(file);
            if (!result.IsSuccess)
            {
                return result;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                this.flakeObject?.SetMemoryOwner(result.Data);
                this.UpdateGetRecentLink();
                return result;
            }
        }

        return new(ZenResult.NoData);
    }

    public async Task<ZenObjectResult<T>> GetObject<T>()
    {
        if (!this.Zen.Started)
        {
            return new(ZenResult.NotStarted);
        }

        ulong file = 0;
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return new(ZenResult.Removed);
            }

            if (this.flakeObject != null && this.flakeObject.TryGetObject(out var obj))
            {// Object
                if (obj is T t)
                {
                    this.UpdateGetRecentLink();
                    return new(ZenResult.Success, t);
                }
                else
                {
                    return new(ZenResult.InvalidCast);
                }
            }

            file = this.flakeFile;
        }

        if (ZenFile.IsValidFile(file))
        {
            var result = await this.Zen.IO.Load(file);
            if (!result.IsSuccess)
            {
                return new(result.Result);
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                this.flakeObject?.SetMemoryOwner(result.Data);
                if (this.flakeObject != null && this.flakeObject.TryGetObject(out var obj))
                {// Object
                    if (obj is T t)
                    {
                        this.UpdateGetRecentLink();
                        return new(ZenResult.Success, t);
                    }
                    else
                    {
                        return new(ZenResult.InvalidCast);
                    }
                }
            }

            return new(result.Result);
        }

        return new(ZenResult.NoData);
    }

    public async Task<ZenDataResult> GetFragment(Identifier fragmentId)
    {
        if (!this.Zen.Started)
        {
            return new(ZenResult.NotStarted);
        }

        ulong file = 0;
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return new(ZenResult.Removed);
            }

            if (this.fragmentObject != null)
            {// Memory
                var fragmentResult = this.fragmentObject.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                if (fragmentResult == FragmentObject.Result.Success)
                {
                    // this.UpdateGetRecentLink();
                    return new(ZenResult.Success, memoryOwner);
                }
                else if (fragmentResult == FragmentObject.Result.NotFound)
                {
                    return new(ZenResult.NoData);
                }
            }

            file = this.fragmentFile;
        }

        if (ZenFile.IsValidFile(file))
        {
            var result = await this.Zen.IO.Load(file);
            if (!result.IsSuccess)
            {
                return result;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
                this.fragmentObject.Load(result.Data);

                var fragmentResult = this.fragmentObject.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                if (fragmentResult == FragmentObject.Result.Success)
                {
                    // this.UpdateGetRecentLink();
                    return new(ZenResult.Success, memoryOwner);
                }
            }
        }

        return new(ZenResult.NoData);
    }

    public async Task<ZenObjectResult<T>> GetFragmentObject<T>(Identifier fragmentId)
    {
        if (!this.Zen.Started)
        {
            return new(ZenResult.NotStarted);
        }

        ulong file = 0;
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return new(ZenResult.Removed);
            }

            if (this.fragmentObject != null)
            {// Memory
                var fragmentResult = this.fragmentObject.TryGetObject(fragmentId, out var obj);
                if (fragmentResult == FragmentObject.Result.Success)
                {
                    if (obj is T t)
                    {
                        // this.UpdateGetRecentLink();
                        return new(ZenResult.Success, t);
                    }
                    else
                    {
                        return new(ZenResult.InvalidCast);
                    }
                }
                else if (fragmentResult == FragmentObject.Result.NotFound)
                {
                    return new(ZenResult.NoData);
                }
            }

            file = this.fragmentFile;
        }

        if (ZenFile.IsValidFile(file))
        {
            var result = await this.Zen.IO.Load(file);
            if (!result.IsSuccess)
            {
                return new(result.Result);
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
                this.fragmentObject.Load(result.Data);

                var fragmentResult = this.fragmentObject.TryGetObject(fragmentId, out var obj);
                if (fragmentResult == FragmentObject.Result.Success)
                {
                    if (obj is T t)
                    {
                        // this.UpdateGetRecentLink();
                        return new(ZenResult.Success, t);
                    }
                    else
                    {
                        return new(ZenResult.InvalidCast);
                    }
                }
            }
        }

        return new(ZenResult.NoData);
    }

    public void Save(bool unload = false)
    {// Skip checking Zen.Started
        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return;
            }

            this.flakeObject?.Save(unload);
            this.fragmentObject?.Save(unload);
        }
    }

    public bool Remove() => this.Zen.Remove(this.Identifier);

    public bool Remove(Identifier fragmentId)
    {
        if (!this.Zen.Started)
        {
            return false;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return false;
            }

            this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
            return this.fragmentObject.Remove(fragmentId);
        }
    }

    public bool TryGetOrAddBlock(out Block block)
    {
        if (!this.Zen.Started)
        {
            block = default;
            return false;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                block = default;
                return false;
            }

            this.nesteGoshujin ??= new();
            block = new(this.Zen, this.nesteGoshujin);
            return true;
        }
    }

    public Zen Zen { get; internal set; } = default!;

    public Identifier Identifier => this.identifier;

    public bool IsRemoved => this.Goshujin == null;

    /*internal ZenResult SetInternal(ReadOnlySpan<byte> data, bool loading)
    {
        if (!this.Zen.Started)
        {
            return ZenResult.NotStarted;
        }
        else if (data.Length > Zen.MaxFlakeSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            if (!loading || this.flakeObject == null)
            {// Not loading or Loading & empty (Skip if loading and not empty)
                this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
                this.flakeObject.SetSpan(data);
            }
        }

        return ZenResult.Success;
    }

    internal ZenResult SetInternal(Identifier fragmentId, ReadOnlySpan<byte> data, bool loading)
    {
        if (data.Length > Zen.MaxFragmentSize)
        {
            return ZenResult.OverSizeLimit;
        }

        lock (this.syncObject)
        {
            if (this.IsRemoved)
            {
                return ZenResult.Removed;
            }

            if (!loading || this.fragmentObject == null)
            {// Not loading or Loading & empty
                this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentObject.SetSpan(fragmentId, data);
            }
            else
            {// Loading & not empty
                return this.fragmentObject.SetSpan(fragmentId, data);
            }
        }
    }*/

    internal void CreateInternal(Flake.GoshujinClass goshujin)
    {// lock (flakeGoshujin)
        lock (this.syncObject)
        {
            if (this.Goshujin == null)
            {
                this.Goshujin = goshujin;
            }
        }
    }

    internal bool RemoveInternal()
    {// lock (flakeGoshujin)
        lock (this.syncObject)
        {
            this.flakeObject?.Unload();
            this.fragmentObject?.Unload();
            this.Goshujin = null;

            this.Zen.IO.Remove(this.flakeFile);
            this.Zen.IO.Remove(this.fragmentFile);
        }

        return true;
    }

    [Key(0)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal Identifier identifier;

    [Key(1)]
    internal ulong flakeFile;

    [Key(2)]
    internal ulong fragmentFile;

    [Key(3)]
    internal Flake.GoshujinClass? nesteGoshujin;

    private object syncObject = new();
    private FlakeObject? flakeObject;
    private FragmentObject? fragmentObject;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateGetRecentLink()
    {// lock (this.syncObject)
        if (this.Goshujin != null)
        {
            this.Goshujin.RecentGetChain.Remove(this);
            this.Goshujin.RecentGetChain.AddFirst(this);
        }
    }
}
