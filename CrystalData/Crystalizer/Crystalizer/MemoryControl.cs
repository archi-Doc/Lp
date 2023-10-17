// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial class MemoryControl
{
    private const int MinimumSize = 1024;
    private const int UnloadIntervalInMilliseconds = 1_000;
    // private const double MarginRatio = 0.1d;

    public MemoryControl(Crystalizer crystalizer)
    {
        this.crystalizer = crystalizer;
    }

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
            // var margin = crystalizer.MemoryUsageLimit * MarginRatio;

            while (!core.IsTerminated)
            {
                if (memoryControl.AvailableMemory > 0)
                {// Sleep
                    await Task.Delay(UnloadIntervalInMilliseconds);
                    continue;
                }

                IStorageData? storageData;
                lock (memoryControl.syncObject)
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
                    lock (memoryControl.syncObject)
                    {// Get the item.
                        if (memoryControl.items.StorageDataChain.FindFirst(storageData) is { } i)
                        {// Remove the item from the chain.
                            memoryControl.memoryUsage -= i.Size;
                            i.Goshujin = null;
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

    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
        public Item(IStorageData storageData, int size)
        {
            this.StorageData = storageData;
            this.Size = size;
        }

        [Link(Type = ChainType.Unordered)]
        public IStorageData StorageData { get; private set; }

        public int Size { get; set; }
    }

    #region FieldAndProperty

    private readonly Crystalizer crystalizer;
    private readonly Item.GoshujinClass items = new();
    private readonly object syncObject = new();

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

    public void Report(IStorageData storageData, int size)
    {
        if (size == 0)
        {// Estimate the data size.
        }

        size = size > MinimumSize ? size : MinimumSize;

        lock (this.items.SyncObject)
        {
            if (this.items.StorageDataChain.FindFirst(storageData) is not { } item)
            {
                item = new(storageData, size);
                item.Goshujin = this.items;
            }
            else
            {
                this.items.UnloadQueueChain.Remove(item);
                this.items.UnloadQueueChain.Enqueue(item);
            }

            this.memoryUsage -= item.Size;
            item.Size = size;
            this.memoryUsage += item.Size;
        }
    }
}
