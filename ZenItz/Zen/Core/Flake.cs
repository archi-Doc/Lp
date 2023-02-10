// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1124 // Do not use regions

public interface IData
{
}

public class Data2 : IData
{
    public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
    {// lock (Flake.syncObject)
        this.Update(this.flakeData.SetSpanInternal(data), clearSavedFlag);
    }

    public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
    {// lock (Flake.syncObject)
        this.Update(this.flakeData.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
    }
}

public partial class Zen<TIdentifier>
{
    public class RootFlake : Flake
    {
        internal RootFlake(Zen<TIdentifier> zen)
            : base(zen, null, default!)
        {
        }

        public override bool IsRemoved => false;
    }

    /// <summary>
    /// <see cref="Flake"/> is an independent class that holds data at a single point in the hierarchical structure.
    /// </summary>
    [TinyhandObject(ExplicitKeyOnly = true, LockObject = nameof(syncObject))]
    [ValueLinkObject]
    public partial class Flake
    {
        public readonly struct DataOperation<TData> : IDisposable
            where TData : IData
        {
            public DataOperation(Flake flake)
            {
                this.flake = flake;
                Monitor.Enter(flake.syncObject, ref this.lockTaken);
                // this.data = data;
            }

            public TData Interface => this.data;

            public void Dispose()
            {
                if (this.lockTaken)
                {
                    Monitor.Exit(this.flake.syncObject);
                }
            }

            private readonly bool lockTaken;
            private readonly Flake flake;
            private readonly TData data;
        }

        public DataOperation<TData> Lock<TData>()
           where TData : IData
        {
            var dataOperation = new DataOperation<TData>(this);
            return dataOperation;
        }

        public DataOperation<TData> LockChild<TData>(TIdentifier id)
            where TData : IData
        {
            Flake? flake;
            lock (this.syncObject)
            {
                if (this.childFlakes == null)
                {
                    return default;
                }

                if (this.childFlakes.IdChain.TryGetValue(id, out flake))
                {// Update GetQueue chain
                    this.childFlakes.GetQueueChain.Remove(flake);
                    this.childFlakes.GetQueueChain.Enqueue(flake);
                }
                else
                {
                    return default;
                }
            }

            return flake.Lock<TData>();
        }

        internal TData GetOrCreateData<TData>()
        {

        }

        [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
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

        public void Save(bool unload = false)
        {
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
                    this.flakeHimo?.UnloadInternal();
                    this.flakeHimo = null;
                    this.fragmentHimo?.UnloadInternal();
                    this.fragmentHimo = null;
                }
            }
        }

        /// <summary>
        /// Removes this <see cref="Flake"/> from the parent and delete the data.
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
                else
                {// Update GetQueue chain
                    this.childFlakes.GetQueueChain.Remove(flake);
                    this.childFlakes.GetQueueChain.Enqueue(flake);
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

                if (this.childFlakes.IdChain.TryGetValue(id, out flake))
                {// Update GetQueue chain
                    this.childFlakes.GetQueueChain.Remove(flake);
                    this.childFlakes.GetQueueChain.Enqueue(flake);
                }

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
            if (data.Length > this.Zen.Options.MaxDataSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                // this.FlakeId = this.TryGetFlakeId(data);

                this.flakeHimo ??= new(this);
                this.flakeHimo.SetSpan(data, true);
            }

            return ZenResult.Success;
        }

        public ZenResult SetDataObject<T>(T obj)
            where T : ITinyhandSerialize<T>
        {
            if (!FlakeFragmentService.TrySerialize(obj, out var memoryOwner))
            {
                return ZenResult.SerializeError;
            }
            else if (memoryOwner.Memory.Length > this.Zen.Options.MaxDataSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                if (obj is IFlake iflake)
                {
                    this.FlakeId = iflake.FlakeId;
                }

                this.flakeHimo ??= new(this);
                this.flakeHimo.SetMemoryOwner(memoryOwner.AsReadOnly(), obj, true);
            }

            return ZenResult.Success;
        }

        public async Task<ZenMemoryResult> GetData()
        {
            ulong file = 0;
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                if (this.flakeHimo != null)
                {// Memory
                    this.flakeHimo.GetMemoryOwner(out var memoryOwner);
                    return new(ZenResult.Success, memoryOwner.Memory); // Skip MemoryOwner.Return()
                }
                else
                {// Load
                    file = this.flakeFile;
                }
            }

            if (ZenHelper.IsValidFile(file))
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

                    this.flakeHimo ??= new(this);
                    this.flakeHimo.SetMemoryOwner(result.Data, null, false);
                    return new(result.Result, result.Data.IncrementAndShare().Memory); // Skip MemoryOwner.Return()
                }
            }

            return new(ZenResult.NoData);
        }

        public async Task<ZenObjectResult<T>> GetDataObject<T>()
            where T : ITinyhandSerialize<T>
        {
            ulong file = 0;
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                if (this.flakeHimo != null)
                {// Memory
                    var result = this.flakeHimo.TryGetObject(out T? obj);
                    return new(result, obj);
                }
                else
                {// Load
                    file = this.flakeFile;
                }
            }

            if (ZenHelper.IsValidFile(file))
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

                    this.flakeHimo ??= new(this);
                    this.flakeHimo.SetMemoryOwner(result.Data, null, false);
                    var flakeResult = this.flakeHimo.TryGetObject(out T? obj);
                    return new(flakeResult, obj);
                }
            }

            return new(ZenResult.NoData);
        }

        #endregion

        #region Fragment

        public ZenResult SetFragment(TIdentifier fragmentId, ReadOnlySpan<byte> span)
        {
            if (span.Length > this.Zen.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                this.fragmentHimo ??= new(this);
                return this.fragmentHimo.SetSpan(fragmentId, span, true);
            }
        }

        public ZenResult SetFragmentObject<T>(TIdentifier fragmentId, T obj)
            where T : ITinyhandSerialize<T>
        {
            if (!FlakeFragmentService.TrySerialize(obj, out var memoryOwner))
            {
                return ZenResult.SerializeError;
            }
            else if (memoryOwner.Memory.Length > this.Zen.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return ZenResult.Removed;
                }

                this.fragmentHimo ??= new(this);
                return this.fragmentHimo.SetMemoryOwner(fragmentId, memoryOwner.AsReadOnly(), obj, true);
            }
        }

        public async Task<ZenMemoryResult> GetFragment(TIdentifier fragmentId)
        {
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
                        return new(ZenResult.Success, memoryOwner.Memory); // Skip MemoryOwner.Return()
                    }
                    else if (fragmentResult == FragmentHimo.Result.NotFound)
                    {
                        return new(ZenResult.NoData);
                    }
                }
                else
                {// Load
                    file = this.fragmentFile;
                }
            }

            if (ZenHelper.IsValidFile(file))
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

                    this.fragmentHimo ??= new(this);
                    this.fragmentHimo.LoadInternal(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        return new(ZenResult.Success, memoryOwner.IncrementAndShare().Memory); // Skip MemoryOwner.Return()
                    }
                }
            }

            return new(ZenResult.NoData);
        }

        public async Task<ZenObjectResult<T>> GetFragmentObject<T>(TIdentifier fragmentId)
            where T : ITinyhandSerialize<T>
        {
            ulong file = 0;
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                if (this.fragmentHimo != null)
                {// Memory
                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out T? obj);
                    return new(fragmentResult, obj);
                }
                else
                {// Load
                    file = this.fragmentFile;
                }
            }

            if (ZenHelper.IsValidFile(file))
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

                    this.fragmentHimo ??= new(this);
                    this.fragmentHimo.LoadInternal(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out T? obj);
                    return new(fragmentResult, obj);
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

                this.fragmentHimo ??= new(this);
                return this.fragmentHimo.RemoveInternal(fragmentId);
            }
        }

        #endregion

        public Zen<TIdentifier> Zen { get; private set; } = default!;

        public Flake? Parent { get; private set; }

        public TIdentifier Identifier => this.identifier;

        public virtual bool IsRemoved => this.Goshujin == null;

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

                this.flakeHimo?.UnloadInternal();
                this.flakeHimo = null;
                this.fragmentHimo?.UnloadInternal();
                this.fragmentHimo = null;
                this.Parent = null;
                this.Goshujin = null;

                this.Zen.IO.Remove(this.flakeFile);
                this.Zen.IO.Remove(this.fragmentFile);
            }

            return true;
        }

        /// <summary>
        /// Save Flake data and unload it from memory.
        /// </summary>
        /// <param name="himoType">Himo type (UchuHimo = All).</param>
        internal void Unload(HimoGoshujinClass.Himo.Type himoType = HimoGoshujinClass.Himo.Type.UchuHimo)
        {
            lock (this.syncObject)
            {
                if (himoType == HimoGoshujinClass.Himo.Type.UchuHimo ||
                    himoType == HimoGoshujinClass.Himo.Type.FlakeHimo)
                {
                    this.flakeHimo?.SaveInternal();
                    this.flakeHimo?.UnloadInternal();
                    this.flakeHimo = null;
                }

                if (himoType == HimoGoshujinClass.Himo.Type.UchuHimo ||
                    himoType == HimoGoshujinClass.Himo.Type.FragmentHimo)
                {
                    this.fragmentHimo?.SaveInternal();
                    this.fragmentHimo?.UnloadInternal();
                    this.fragmentHimo = null;
                }
            }
        }

        [Key(0)]
        [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
        [Link(Name = "OrderedId", Type = ChainType.Ordered)]
        internal TIdentifier identifier = default!;

        [Key(1)]
        internal ulong flakeFile;

        [Key(2)]
        internal ulong fragmentFile;

        [Key(3)]
        internal Flake.GoshujinClass? childFlakes;

        [Key(4)]
        public int FlakeId { get; private set; }

        private int TryGetFlakeId(ReadOnlySpan<byte> data)
        {
            try
            {
                var reader = new TinyhandReader(data);
                if (reader.TryReadArrayHeader(out var count) && count == 2)
                {
                    return reader.ReadInt32();
                }
            }
            catch
            {
            }

            return 0;
        }

        private object syncObject = new();
        private FlakeHimo? flakeHimo;
        private FragmentHimo? fragmentHimo;
    }
}
