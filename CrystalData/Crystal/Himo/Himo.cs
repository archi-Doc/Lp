﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

using System.Runtime.CompilerServices;

namespace CrystalData;

public partial class HimoGoshujinClass
{
    public const int UnloadInterval = 100; // 100 ms
    public const int UnloadNumber = 10;
    public const long MemoryMargin = 1024 * 1024 * 100; // 100 MB

    [ValueLinkObject]
    public partial class Himo
    {
        [Link(Name = "UnloadQueue", Type = ChainType.QueueList)] // Manages the order of unloading data from memory
        public Himo(IDataInternal flakeInternal)
        {
            this.dataInternal = flakeInternal;
            this.himoGoshujin = flakeInternal.CrystalInternal.HimoGoshujin;
        }

        public virtual int Id { get; }

        public void UpdateHimo(int newSize)
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

                this.himoGoshujin.memoryUsage += newSize - this.currentSize;
                this.currentSize = newSize;
                if (this.himoGoshujin.memoryUsage > this.dataInternal.Options.MemorySizeLimit)
                {
                    unloadFlag = true;
                }
            }

            if (unloadFlag)
            {
                this.himoGoshujin.Unload();
            }
        }

        public void UpdateHimo()
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

        public void RemoveHimo()
        {// using (Flake.semaphore)
            lock (this.himoGoshujin.syncObject)
            {
                this.Goshujin = null;
                this.himoGoshujin.memoryUsage -= this.currentSize;
                this.currentSize = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCurrentSize(int difference)
        {
            if (difference != 0)
            {
                lock (this.himoGoshujin.syncObject)
                {
                    this.currentSize += difference;
                    this.himoGoshujin.memoryUsage += difference;
                }
            }
        }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        protected internal IDataInternal dataInternal;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
        private HimoGoshujinClass himoGoshujin;
        private int currentSize; // Current memory usage
    }

    public HimoGoshujinClass(ICrystalInternal crystalInternal)
    {
        this.crystalInternal = crystalInternal;
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
        var limit = Math.Max(MemoryMargin, this.crystalInternal.Options.MemorySizeLimit - MemoryMargin);
        if (Volatile.Read(ref this.memoryUsage) <= limit)
        {
            return;
        }

        var array = new (IDataInternal FlakeInternal, int Id)[UnloadNumber];
        do
        {
            int count;
            lock (this.syncObject)
            {// Get flake/himo type array.
                this.goshujin.UnloadQueueChain.TryPeek(out var himo);
                for (count = 0; himo != null && count < UnloadNumber; count++)
                {
                    array[count].FlakeInternal = himo.dataInternal;
                    array[count].Id = himo.Id;
                    himo = himo.UnloadQueueLink.Next;
                }
            }

            for (var i = 0; i < count; i++)
            {
                array[i].FlakeInternal.SaveData(array[i].Id, true);
            }
        }
        while (Volatile.Read(ref this.memoryUsage) > limit);
    }

    internal void Clear()
    {
        lock (this.syncObject)
        {
            this.goshujin.Clear();
            this.memoryUsage = 0;
        }
    }

    internal long MemoryUsage => this.memoryUsage;

    private ICrystalInternal crystalInternal;

    private object syncObject = new();
    private long memoryUsage; // lock(this.syncObject)
    private Himo.GoshujinClass goshujin = new(); // lock(this.syncObject)
    private HimoTaskCore? taskCore;
}