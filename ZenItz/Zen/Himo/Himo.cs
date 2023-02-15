// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class HimoGoshujinClass
{
    public const int UnloadInterval = 100; // 100 ms
    public const int UnloadNumber = 10;
    public const long MemoryMargin = 1024 * 1024 * 100; // 100 MB

    [ValueLinkObject]
    internal partial class Himo
    {
        [Link(Name = "UnloadQueue", Type = ChainType.QueueList)] // Manages the order of unloading data from memory
        public Himo(IFlakeInternal flakeInternal)
        {
            this.flakeInternal = flakeInternal;
            this.himoGoshujin = flakeInternal.ZenInternal.HimoGoshujin;
        }

        public virtual int Id { get; }

        internal void Update(int memoryDifference)
        {// using (Flake.semaphore)
            var unloadFlag = false;

            lock (this.himoGoshujin.syncObject)
            {
                if (this.Goshujin == null)
                {// New
                    this.Goshujin = this.himoGoshujin.goshujin;
                }
                else
                {// Update
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                }

                this.himoGoshujin.memoryUsage += memoryDifference;
                if (this.himoGoshujin.memoryUsage > this.flakeInternal.Options.MemorySizeLimit)
                {
                    unloadFlag = true;
                }
            }

            if (unloadFlag)
            {
                this.himoGoshujin.Unload();
            }
        }

        internal void Update()
        {// using (Flake.semaphore)
            lock (this.himoGoshujin.syncObject)
            {
                if (this.Goshujin == null)
                {// New
                    this.Goshujin = this.himoGoshujin.goshujin;
                }
                else
                {// Update
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                }
            }
        }

        internal void Remove(int memoryDifference)
        {// using (Flake.semaphore)
            lock (this.himoGoshujin.syncObject)
            {
                this.Goshujin = null;
                this.himoGoshujin.memoryUsage += memoryDifference;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Change(int memoryDifference)
        {
            if (memoryDifference != 0)
            {
                lock (this.himoGoshujin.syncObject)
                {
                    this.himoGoshujin.memoryUsage += memoryDifference;
                }
            }
        }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        protected internal IFlakeInternal flakeInternal;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
        private HimoGoshujinClass himoGoshujin;
    }

    public HimoGoshujinClass(IZenInternal zenInternal)
    {
        this.zenInternal = zenInternal;
    }

    internal void Start()
    {
        this.taskCore ??= new(ThreadCore.Root, this);
    }

    internal void Stop()
    {
        if (this.taskCore is { } taskCore)
        {
            this.taskCore = null;
            taskCore.Terminate();
        }
    }

    internal void Unload()
    {
        var limit = Math.Max(MemoryMargin, this.zenInternal.Options.MemorySizeLimit - MemoryMargin);
        if (Volatile.Read(ref this.memoryUsage) <= limit)
        {
            return;
        }

        var array = new (IFlakeInternal FlakeInternal, int Id)[UnloadNumber];
        do
        {
            int count;
            lock (this.syncObject)
            {// Get flake/himo type array.
                this.goshujin.UnloadQueueChain.TryPeek(out var himo);
                for (count = 0; himo != null && count < UnloadNumber; count++)
                {
                    array[count].FlakeInternal = himo.flakeInternal;
                    array[count].Id = himo.Id;
                    himo = himo.UnloadQueueLink.Next;
                }
            }

            for (var i = 0; i < count; i++)
            {
                array[i].FlakeInternal.Save(array[i].Id, true);
            }
        }
        while (Volatile.Read(ref this.memoryUsage) > limit);
    }

    internal void Clear()
    {
        lock (this.syncObject)
        {
            this.goshujin.Clear();
        }
    }

    internal long MemoryUsage => this.memoryUsage;

    private IZenInternal zenInternal;

    private object syncObject = new();
    private long memoryUsage; // lock(this.syncObject)
    private Himo.GoshujinClass goshujin = new(); // lock(this.syncObject)
    private HimoTaskCore? taskCore;
}
