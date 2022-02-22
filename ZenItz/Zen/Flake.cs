// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

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

    public ZenResult Set(ReadOnlySpan<byte> data) => this.SetInternal(data, false);

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

    public bool TryRemove() => this.Zen.TryRemove(this.Identifier);

    public Zen Zen { get; internal set; } = default!;

    public Identifier Identifier => this.identifier;

    public bool IsRemoved => this.Goshujin == null;

    internal ZenResult SetInternal(ReadOnlySpan<byte> data, bool loading)
    {
        if (data.Length > Zen.MaxFlakeSize)
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
    }

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
            if (this.Goshujin == null)
            {
                return false;
            }
            else
            {
                this.Goshujin = null;
                return true;
            }
        }
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
