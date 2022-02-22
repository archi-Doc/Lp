// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class Flake
{
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

    internal ZenResult Set(ByteArrayPool.MemoryOwner dataToBeMoved)
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
    }

    public ZenResult SetObject<T>(T obj)
    {
        byte[]? byteArray;
        try
        {
            byteArray = TinyhandSerializer.Serialize<T>(obj);
        }
        catch
        {
            return ZenResult.SerializationError;
        }

        var result = this.Set(byteArray);
        return result;
    }

    public ZenResult Set(Identifier fragmentId, ReadOnlySpan<byte> data)
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

    internal ZenResult Set(Identifier fragmentId, ByteArrayPool.MemoryOwner data)
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
                if (!this.IsRemoved)
                {
                    this.flakeObject?.SetMemoryOwner(result.Data);
                }
            }

            return result;
        }

        return new(ZenResult.NoData);
    }

    public async Task<ZenObjectResult<T>> GetObject<T>()
    {
        var result = await this.Get().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return new(result.Result);
        }

        if (!TinyhandSerializer.TryDeserialize<T>(result.Data.Memory, out var obj))
        {
            result.Data.Return();
            return new(ZenResult.DeserializationError);
        }

        result.Data.Return();
        return new(ZenResult.Success, obj);
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
    [Link(Primary = true, Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal Identifier identifier;

    [Key(1)]
    internal ulong flakeFile;

    [Key(2)]
    internal ulong fragmentFile;

    private object syncObject = new();
    private FlakeObject? flakeObject;
    private FragmentObject? fragmentObject;
}
