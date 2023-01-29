// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1124 // Do not use regions

public partial class Zen<TIdentifier>
{
    /// <summary>
    /// <see cref="Flake"/> is an independent class that holds data at a single point in the hierarchical structure.
    /// </summary>
    [TinyhandObject(ExplicitKeyOnly = true)]
    [ValueLinkObject]
    public partial class Flake : ITinyhandSerializationCallback
    {
        // [Link(Primary = true, Name = "RecentGet", Type = ChainType.LinkedList)]
        internal Flake()
        {
        }

        internal Flake(Zen<TIdentifier> zen, Flake? parent, TIdentifier identifier)
        {
            this.Zen = zen;
            this.Parent = parent;
            this.identifier = identifier;
        }

        #region Main

        public void OnBeforeSerialize()
        {
            Monitor.Enter(this.syncObject);
        }

        public void OnAfterDeserialize()
        {
            Monitor.Exit(this.syncObject);
        }

        public void Save(bool unload = false)
        {// Skip checking Zen.Started
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return;
                }

                if (this.childFlakes != null)
                {
                    foreach (var x in this.childFlakes)
                    {
                        x.Save(unload);
                    }
                }

                this.flakeHimo?.SaveInternal();
                this.fragmentHimo?.SaveInternal();
                if (unload)
                {
                    this.UnloadInternal();
                }
            }
        }

        /// <summary>
        /// Removes this <see cref="Flake"/> from the parent and erase the data.
        /// </summary>
        /// <returns><see langword="true"/>; this <see cref="Flake"/> is successfully removed.</returns>
        public bool Remove()
        {
            if (this.Parent == null)
            {// The root flake cannot be removed directly.
                return false;
            }

            lock (this.Parent.syncObject)
            {
                return this.RemoveInternal();
            }
        }

        #endregion

        #region Child

        public Flake GetOrCreateChild(TIdentifier id)
        {
            Flake? flake;
            lock (this.syncObject)
            {
                this.childFlakes ??= new();
                if (!this.childFlakes.IdChain.TryGetValue(id, out flake))
                {
                    flake = new Flake(this.Zen, this, id);
                    this.childFlakes.Add(flake);
                }
            }

            return flake;
        }

        public Flake? TryGetChild(TIdentifier id)
        {
            Flake? flake;
            lock (this.syncObject)
            {
                if (this.childFlakes == null)
                {
                    return null;
                }

                this.childFlakes.IdChain.TryGetValue(id, out flake);
                return flake;
            }
        }

        public bool RemoveChild(TIdentifier id)
        {
            lock (this.syncObject)
            {
                if (this.childFlakes == null)
                {
                    return false;
                }

                if (this.childFlakes.IdChain.TryGetValue(id, out var flake))
                {
                    return flake.RemoveInternal();
                }
            }

            return false;
        }

        #endregion

        #region Data

        public ZenResult SetData(ReadOnlySpan<byte> data)
        {
            if (!this.Zen.Started)
            {
                return ZenResult.NotStarted;
            }
            else if (data.Length > this.Zen.Options.MaxDataSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                this.flakeHimo ??= new(this);
                this.flakeHimo.SetSpan(data, true);
            }

            return ZenResult.Success;
        }

        public ZenResult SetDataObject(object obj)
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

                this.flakeHimo ??= new(this, this.Zen.HimoGoshujin);
                this.flakeHimo.SetObject(obj);
            }

            return ZenResult.Success;
        }

        public async Task<ZenDataResult> GetData()
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

                if (this.flakeHimo != null && this.flakeHimo.TryGetMemoryOwner(out var memoryOwner))
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
                    if (this.IsRemoved)
                    {
                        return new(ZenResult.Removed);
                    }

                    this.flakeHimo?.SetMemoryOwner(result.Data);
                    return result;
                }
            }

            return new(ZenResult.NoData);
        }

        public async Task<ZenObjectResult<T>> GetDataObject<T>()
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

                if (this.flakeHimo != null && this.flakeHimo.TryGetObject(out var obj))
                {// Object
                    if (obj is T t)
                    {
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

                    this.flakeHimo?.SetMemoryOwner(result.Data);
                    if (this.flakeHimo != null && this.flakeHimo.TryGetObject(out var obj))
                    {// Object
                        if (obj is T t)
                        {
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

        #endregion

        #region Fragment

        public ZenResult SetFragment(TIdentifier fragmentId, ReadOnlySpan<byte> data)
        {
            if (!this.Zen.Started)
            {
                return ZenResult.NotStarted;
            }
            else if (data.Length > this.Zen.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                this.fragmentHimo ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentHimo.SetSpan(fragmentId, data);
            }
        }

        public ZenResult SetFragmentObject(TIdentifier fragmentId, object obj)
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

                this.fragmentHimo ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentHimo.SetObject(fragmentId, obj);
            }
        }

        public async Task<ZenDataResult> GetFragment(TIdentifier fragmentId)
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

                if (this.fragmentHimo != null)
                {// Memory
                    var fragmentResult = this.fragmentHimo.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        return new(ZenResult.Success, memoryOwner);
                    }
                    else if (fragmentResult == FragmentHimo.Result.NotFound)
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

                    this.fragmentHimo ??= new(this, this.Zen.FragmentObjectGoshujin);
                    this.fragmentHimo.Load(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        return new(ZenResult.Success, memoryOwner);
                    }
                }
            }

            return new(ZenResult.NoData);
        }

        public async Task<ZenObjectResult<T>> GetFragmentObject<T>(TIdentifier fragmentId)
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

                if (this.fragmentHimo != null)
                {// Memory
                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out var obj);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        if (obj is T t)
                        {
                            return new(ZenResult.Success, t);
                        }
                        else
                        {
                            return new(ZenResult.InvalidCast);
                        }
                    }
                    else if (fragmentResult == FragmentHimo.Result.NotFound)
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

                    this.fragmentHimo ??= new(this, this.Zen.FragmentObjectGoshujin);
                    this.fragmentHimo.Load(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out var obj);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        if (obj is T t)
                        {
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

        public bool RemoveFragment(TIdentifier fragmentId)
        {
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return false;
                }

                this.fragmentHimo ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentHimo.Remove(fragmentId);
            }
        }

        #endregion

        public Zen<TIdentifier> Zen { get; private set; } = default!;

        public Flake? Parent { get; private set; }

        public TIdentifier Identifier => this.identifier;

        public bool IsRemoved => this.Goshujin == null && this.Parent != null;

        internal void DeserializePostProcess(Zen<TIdentifier> zen, Flake? parent = null)
        {
            this.Zen = zen;
            this.Parent = parent;

            if (this.childFlakes != null)
            {
                foreach (var x in this.childFlakes)
                {
                    x.DeserializePostProcess(zen, this);
                }
            }
        }

        internal bool RemoveInternal()
        {// lock (Parent.syncObject)
            lock (this.syncObject)
            {
                if (this.childFlakes != null)
                {
                    foreach (var x in this.childFlakes.ToArray())
                    {
                        x.RemoveInternal();
                    }

                    this.childFlakes = null;
                }

                this.flakeHimo?.Unload();
                this.fragmentHimo?.Unload();
                this.Parent = null;
                this.Goshujin = null;

                this.Zen.IO.Remove(this.flakeFile);
                this.Zen.IO.Remove(this.fragmentFile);
            }

            return true;
        }

        internal void Unload()
        {
            lock (this.syncObject)
            {
                this.flakeHimo?.SaveInternal();
                this.fragmentHimo?.SaveInternal();

                this.UnloadInternal();
            }
        }

        internal void UnloadInternal()
        {
            int memoryDifference = 0;
            if (this.flakeHimo != null)
            {
                memoryDifference += this.flakeHimo.UnloadInternal();
                this.flakeHimo = null;
            }

            if (this.fragmentHimo != null)
            {
                memoryDifference += this.fragmentHimo.UnloadInternal();
                this.fragmentHimo = null;
            }

            this.RemoveHimo(memoryDifference);
        }

        [Key(0)]
        [Link(Primary = true, Name = "Id", NoValue = true, Type = ChainType.Unordered)]
        [Link(Name = "OrderedId", Type = ChainType.Ordered)]
        internal TIdentifier identifier = default!;

        [Key(1)]
        internal ulong flakeFile;

        [Key(2)]
        internal ulong fragmentFile;

        [Key(3)]
        internal Flake.GoshujinClass? childFlakes;

        #region Himo

        internal HimoGoshujinClass.Himo Himo => this.himo ??= new(this.Zen.HimoGoshujin, this);

        internal void RemoveHimo(int memoryDifference)
        {
            this.Himo.Remove(memoryDifference);
            this.himo = null;
        }

        #endregion

        private object syncObject = new();
        private HimoGoshujinClass.Himo? himo;
        private FlakeHimo? flakeHimo;
        private FragmentHimo? fragmentHimo;
    }
}
