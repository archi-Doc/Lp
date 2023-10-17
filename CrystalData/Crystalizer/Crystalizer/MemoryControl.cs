﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial class MemoryControl
{
    private const int MinimumDataSize = 1024;
    private const int UnloadIntervalInMilliseconds = 1_000;

    public MemoryControl(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
        this.unloader = new(this);
    }

    #region Unloader

    private class Unloader : TaskCore
    {
        public Unloader(MemoryControl memoryControl)
            : base(null, Process)
        {
            this.memoryControl = memoryControl;
        }

        private static async Task Process(object? parameter)
        {
            var core = (Unloader)parameter!;
            var memoryControl = core.memoryControl;
            var crystalizer = core.memoryControl.crystalizer;

            while (!core.IsTerminated)
            {
                if (memoryControl.MemoryUsage < crystalizer.MemoryUsageLimit)
                {// Sleep
                    await core.Delay(UnloadIntervalInMilliseconds);
                    continue;
                }

                IStorageData? storageData;
                lock (memoryControl.items.SyncObject)
                {// Get the first item.
                    if (memoryControl.items.UnloadQueueChain.TryPeek(out var item))
                    {
                        memoryControl.items.UnloadQueueChain.Remove(item);
                        memoryControl.items.UnloadQueueChain.Enqueue(item);
                        storageData = item.StorageData;
                    }
                    else
                    {// No item
                        storageData = null;
                    }
                }

                if (storageData is null)
                {
                    await Task.Delay(UnloadIntervalInMilliseconds);
                    continue;
                }

                if (await storageData.Unload())
                {// Success
                    lock (memoryControl.items.SyncObject)
                    {// Get the item.
                        if (memoryControl.items.StorageDataChain.FindFirst(storageData) is { } item)
                        {// Remove the item from the chain.
                            memoryControl.memoryUsage -= item.DataSize;
                            item.Goshujin = null;
                        }
                    }
                }
                else
                {// Failure
                }
            }
        }

        private readonly MemoryControl memoryControl;
    }

    #endregion

    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
        public Item(IStorageData storageData, int dataSize)
        {
            this.StorageData = storageData;
            this.DataSize = dataSize;
        }

        [Link(Type = ChainType.Unordered)]
        public IStorageData StorageData { get; private set; }

        public int DataSize { get; set; }
    }

    #region FieldAndProperty

    private readonly Crystalizer crystalizer;
    private readonly Unloader unloader;

    // Items
    private readonly Item.GoshujinClass items = new();
    private long memoryUsage;

    #endregion

    public long MemoryUsage => Volatile.Read(ref this.memoryUsage);

    public long AvailableMemory
    {
        get
        {
            var available = this.crystalizer.MemoryUsageLimit - this.MemoryUsage;
            available = available > 0 ? available : 0;
            return available;
        }
    }

    public void Report(Type dataType, int dataSize)
    {
    }

    public void Register(IStorageData storageData, int dataSize)
    {
        if (dataSize == 0)
        {// Estimate the data size.
        }

        dataSize = dataSize > MinimumDataSize ? dataSize : MinimumDataSize;

        lock (this.items.SyncObject)
        {
            if (this.items.StorageDataChain.FindFirst(storageData) is not { } item)
            {
                item = new(storageData, dataSize);
                item.Goshujin = this.items;
            }
            else
            {
                this.items.UnloadQueueChain.Remove(item);
                this.items.UnloadQueueChain.Enqueue(item);
            }

            this.memoryUsage -= item.DataSize;
            item.DataSize = dataSize;
            this.memoryUsage += item.DataSize;
        }
    }
}
