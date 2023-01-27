// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ZenItz;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1124 // Do not use regions

public partial class Zen<TIdentifier>
{
    [TinyhandObject(ExplicitKeyOnly = true)]
    [ValueLinkObject]
    public partial class Flake
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

        public void DeserializePostProcess(Zen<TIdentifier> zen, Flake? parent = null)
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

                this.flakeObject?.Save(unload);
                this.fragmentObject?.Save(unload);
            }
        }

        /// <summary>
        /// Removes this <see cref="Flake"/> from the parent and erase the data.
        /// </summary>
        /// <returns><see langword="true"/>; this <see cref="Flake"/> is successfully removed.</returns>
        public bool Remove()
        {
            var syncObject = this.Parent?.syncObject;
            if (syncObject != null)
            {
                lock (syncObject)
                {
                    return this.RemoveInternal();
                }
            }
            else
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

                this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
                this.flakeObject.SetSpan(data);
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

                this.flakeObject ??= new(this, this.Zen.FlakeObjectGoshujin);
                this.flakeObject.SetObject(obj);
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

                this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentObject.SetSpan(fragmentId, data);
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

                this.fragmentObject ??= new(this, this.Zen.FragmentObjectGoshujin);
                return this.fragmentObject.SetObject(fragmentId, obj);
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

        public bool RemoveFragment(TIdentifier fragmentId)
        {
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

        #endregion

        public Zen<TIdentifier> Zen { get; private set; } = default!;

        public Flake? Parent { get; private set; }

        public TIdentifier TIdentifier => this.identifier;

        public bool IsRemoved => this.Goshujin == null && this.Parent != null;

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

        internal ZenResult SetInternal(TIdentifier fragmentId, ReadOnlySpan<byte> data, bool loading)
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

                this.flakeObject?.Unload();
                this.fragmentObject?.Unload();
                this.Parent = null;
                this.Goshujin = null;

                this.Zen.IO.Remove(this.flakeFile);
                this.Zen.IO.Remove(this.fragmentFile);
            }

            return true;
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

        internal object syncObject = new();
        private FlakeObject? flakeObject;
        private FragmentObject? fragmentObject;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateGetRecentLink()
        {// lock (this.syncObject)
            if (this.Goshujin != null)
            {
                // this.Goshujin.RecentGetChain.Remove(this);
                // this.Goshujin.RecentGetChain.AddFirst(this);
            }
        }
    }
}
