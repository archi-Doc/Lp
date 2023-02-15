// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace ZenItz;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1124 // Do not use regions

public partial class Zen<TIdentifier>
{
    /// <summary>
    /// <see cref="Flake"/> is an independent class that holds data at a single point in the hierarchical structure.
    /// </summary>
    [TinyhandObject(ExplicitKeyOnly = true, LockObject = "semaphore")]
    [ValueLinkObject]
    public partial class Flake : IFlakeInternal
    {
        public struct DataOperation<TData> : IDisposable
            where TData : IData
        {
            public DataOperation(Flake flake)
            {
                this.flake = flake;
            }

            public bool IsValid => this.data != null;

            public TData? Data => this.data;

            public void Dispose()
            {
                this.Exit();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool Enter()
            {
                if (!this.lockTaken)
                {
                    this.lockTaken = this.flake.semaphore.Enter();
                }

                return this.lockTaken;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Exit()
            {
                this.data = default;
                if (this.lockTaken)
                {
                    this.flake.semaphore.Exit();
                    this.lockTaken = false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetData(TData data)
                => this.data = data;

            private readonly Flake flake;
            private TData? data;
            private bool lockTaken;
        }

        public DataOperation<TData> Lock<TData>()
           where TData : IData
        {
            var operation = new DataOperation<TData>(this);

            operation.Enter();
            if (this.IsRemoved)
            {// Removed
                operation.Exit();
                return operation;
            }

            var result = this.GetOrCreateDataObject<TData>();
            if (result.DataObject.Data is not TData data)
            {// No data
                operation.Exit();
                return operation;
            }

            operation.SetData(data);
            return operation;
        }

        public DataOperation<TData> LockChild<TData>(TIdentifier id)
            where TData : IData
        {
            Flake? flake;
            using (this.semaphore.Lock())
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

        #region IFlakeInternal

        IZenInternal IFlakeInternal.ZenInternal => this.Zen;

        ZenOptions IFlakeInternal.Options => this.Zen.Options;

        void IFlakeInternal.SaveInternal<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// using (this.semaphore.Lock())
            var id = TData.StaticId;
            for (var i = 0; i < this.dataObject.Length; i++)
            {
                if (this.dataObject[i].Id == id)
                {
                    this.Zen.IO.Save(ref this.dataObject[i].File, memoryOwner);
                    return;
                }
            }
        }

        async Task<ZenMemoryOwnerResult> IFlakeInternal.LoadInternal<TData>()
        {// using (this.semaphore.Lock())
            var dataObject = this.TryGetDataObject<TData>();
            if (!dataObject.IsValid)
            {
                return new(ZenResult.NoData);
            }

            var file = dataObject.File;
            if (!ZenHelper.IsValidFile(file))
            {
                return new(ZenResult.NoData);
            }

            return await this.Zen.IO.Load(file);

            /*var result = default(ZenMemoryOwnerResult);
            try
            {
                this.semaphore.Exit();
                result = await this.Zen.IO.Load(file);
            }
            finally
            {
                this.semaphore.Enter();
            }

            if (this.IsRemoved)
            {
                return new(ZenResult.Removed);
            }

            return result;*/
        }

        void IFlakeInternal.RemoveInternal<TData>()
        {// using (this.semaphore.Lock())
            var dataObject = this.TryGetDataObject<TData>();
            if (dataObject.IsValid)
            {
                this.Zen.IO.Remove(dataObject.File);
                return;
            }
        }

        /// <summary>
        /// Called from outside Flake, unloads DataObjects with matching id.
        /// </summary>
        /// <param name="id">The specified id.</param>
        /// <param name="unload"><see langword="true"/>; unload data.</param>
        void IFlakeInternal.Save(int id, bool unload)
        {
            using (this.semaphore.Lock())
            {
                for (var i = 0; i < this.dataObject.Length; i++)
                {
                    if (this.dataObject[i].Id == id)
                    {
                        this.dataObject[i].Save();
                        this.dataObject[i].Unload();
                        return;
                    }
                }
            }
        }

        #endregion

        #region Main

        public void Save(bool unload = false)
        {
            using (this.semaphore.Lock())
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

                foreach (var x in this.dataObject)
                {
                    x.Save();
                    if (unload)
                    {
                        x.Unload();
                    }
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

            using (this.Parent.semaphore.Lock())
            {
                return this.RemoveInternal();
            }
        }

        #endregion

        #region Child

        public Flake GetOrCreateChild(TIdentifier id)
        {
            Flake? flake;
            using (this.semaphore.Lock())
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
            using (this.semaphore.Lock())
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
            using (this.semaphore.Lock())
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
            using (this.semaphore.Lock())
            {
                if (this.childFlakes != null)
                {
                    foreach (var x in this.childFlakes.ToArray())
                    {
                        x.RemoveInternal();
                    }

                    this.childFlakes = null;
                }

                foreach (var x in this.dataObject)
                {
                    x.Remove(this.Zen.IO);
                }

                this.dataObject = Array.Empty<DataObject>();
                this.Parent = null;
                this.Goshujin = null;
            }

            return true;
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

        [Key(5)]
        private DataObject[] dataObject = Array.Empty<DataObject>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (DataObject DataObject, bool Created) GetOrCreateDataObject<TData>()
            where TData : IData
        {// using (this.semaphore.Lock())
            var id = TData.StaticId;
            foreach (var x in this.dataObject)
            {
                if (x.Id == id)
                {
                    return (x, x.GetOrCreateObject(this).Created);
                }
            }

            var newObject = default(DataObject);
            newObject.Id = id;
            var result = newObject.GetOrCreateObject(this);
            if (result.Data == null)
            {
                return default;
            }

            var n = this.dataObject.Length;
            Array.Resize(ref this.dataObject, n + 1);
            this.dataObject[n] = newObject;
            return (newObject, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DataObject TryGetDataObject<TData>()
            where TData : IData
        {// using (this.semaphore.Lock())
            var id = TData.StaticId;
            for (var i = 0; i < this.dataObject.Length; i++)
            {
                if (this.dataObject[i].Id == id)
                {
                    return this.dataObject[i];
                }
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DataObject TryGetDataObject(int id)
        {// using (this.semaphore.Lock())
            for (var i = 0; i < this.dataObject.Length; i++)
            {
                if (this.dataObject[i].Id == id)
                {
                    return this.dataObject[i];
                }
            }

            return default;
        }

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

        private readonly SemaphoreLock semaphore = new();
    }
}
