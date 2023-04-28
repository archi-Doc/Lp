// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

using System.Runtime.CompilerServices;
using Arc.Collections;

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
        public Himo(IDataInternal dataInternal)
        {
            this.dataInternal = dataInternal;
            this.himoGoshujin = dataInternal.BigCrystal.Crystalizer.Himo;
        }

        public virtual ushort Id { get; }

        public void UpdateHimo(int newSize)
        {// using (Object.semaphore)
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
                if (this.himoGoshujin.memoryUsage > this.dataInternal.BigCrystal.BigCrystalConfiguration.MemorySizeLimit)
                {
                    unloadFlag = true;
                }
            }

            if (unloadFlag)
            {
                _ = this.himoGoshujin.unloadData.Run();
            }
        }

        public void UpdateHimo()
        {// using (Object.semaphore)
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
        {// using (Object.semaphore)
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

    public HimoGoshujinClass(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;

        this.unloadData = new(() => this.UnloadData());
        this.unloadParent = new(() => this.UnloadParent());
    }

    public UnorderedLinkedList<BaseData>.Node AddParentData(BaseData data)
    {// data.semaphore.Lock()
        UnorderedLinkedList<BaseData>.Node node;
        var unloadFlag = false;

        lock (this.syncParentData)
        {
            node = this.parentDataList.AddLast(data);
            if (this.parentDataList.Count > this.crystalizer.MaxParentInMemory)
            {
                unloadFlag = true;
            }
        }

        if (unloadFlag)
        {
            _ = this.unloadParent.Run();
        }

        return node;
    }

    public void RemoveParentData(UnorderedLinkedList<BaseData>.Node node)
    {// data.semaphore.Lock()
        lock (this.syncParentData)
        {
            this.parentDataList.Remove(node);
        }
    }

    public void Unload()
    {
        this.UnloadData();
        this.UnloadParent();
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

    private void UnloadData()
    {
        var limit = Math.Max(MemoryMargin, this.crystalizer.MemorySizeLimit - MemoryMargin);
        if (Volatile.Read(ref this.memoryUsage) <= limit)
        {
            return;
        }

        var array = new (IDataInternal? DataInternal, ushort Id)[UnloadNumber];
        do
        {
            int count;
            lock (this.syncObject)
            {// Get flake/himo type array.
                this.goshujin.UnloadQueueChain.TryPeek(out var himo);
                for (count = 0; himo != null && count < UnloadNumber; count++)
                {
                    array[count].DataInternal = himo.dataInternal;
                    array[count].Id = himo.Id;
                    himo = himo.UnloadQueueLink.Next;
                }
            }

            for (var i = 0; i < count; i++)
            {
                array[i].DataInternal?.SaveDatum(array[i].Id, true);
            }
        }
        while (Volatile.Read(ref this.memoryUsage) > limit);
    }

    private void UnloadParent()
    {
        if (this.parentDataList.Count <= this.crystalizer.MaxParentInMemory)
        {
            return;
        }

        var array = new BaseData?[UnloadNumber];
        do
        {
            int count;
            lock (this.syncParentData)
            {
                var node = this.parentDataList.First;
                for (count = 0; count < UnloadNumber; count++)
                {
                    if (node == null)
                    {
                        break;
                    }

                    array[count] = node.Value;
                    node = node.Next;
                    // this.parentDataList.Remove(node);
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (array[i] != null)
                {
                    array[i]!.Save(true);
                    array[i] = null;
                }
            }
        }
        while (this.parentDataList.Count > this.crystalizer.MaxParentInMemory);
    }

    private Crystalizer crystalizer;

    private object syncObject = new();
    private long memoryUsage; // lock(this.syncObject)
    private Himo.GoshujinClass goshujin = new(); // lock(this.syncObject)

    // private HimoTaskCore? taskCore;
    private UniqueWork unloadData;
    private UniqueWork unloadParent;

    private object syncParentData = new();
    private UnorderedLinkedList<BaseData> parentDataList = new();
}
