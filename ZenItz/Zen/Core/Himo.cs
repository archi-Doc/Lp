// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal partial class HimoGoshujinClass
    {
        public const int UnloadInterval = 100; // 100 ms
        public const int UnloadNumber = 10;
        public const long MemoryMargin = 1024 * 1024 * 100; // 100 MB

        [ValueLinkObject]
        internal partial class Himo
        {
            public enum Type
            {
                UchuHimo,
                FlakeHimo,
                FragmentHimo,
            }

            [Link(Name = "UnloadQueue", Type = ChainType.QueueList)] // Manages the order of unloading data from memory
            public Himo(Flake flake)
            {
                this.himoGoshujin = flake.Zen.HimoGoshujin;
                this.flake = flake;
            }

            public Type HimoType { get; protected set; }

            internal void Update(int memoryDifference)
            {// lock (Flake.syncObject)
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

                    this.himoGoshujin.totalSize += memoryDifference;
                    if (this.himoGoshujin.totalSize > this.flake.Zen.Options.MemorySizeLimit)
                    {
                        unloadFlag = true;
                    }
                }

                if (unloadFlag)
                {
                    this.himoGoshujin.Unload();
                }
            }

            internal void Remove(int memoryDifference)
            {// lock (Flake.syncObject)
                lock (this.himoGoshujin.syncObject)
                {
                    this.Goshujin = null;
                    this.himoGoshujin.totalSize += memoryDifference;
                }
            }

            internal Flake Flake => this.flake;

            private Flake flake;
            private HimoGoshujinClass himoGoshujin;
        }

        public HimoGoshujinClass(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
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

        public Zen<TIdentifier> Zen { get; }

        internal void Unload()
        {
            var limit = Math.Max(MemoryMargin, this.Zen.Options.MemorySizeLimit - MemoryMargin);
            if (Volatile.Read(ref this.totalSize) <= limit)
            {
                return;
            }

            var array = new (Flake Flake, Himo.Type HimoType)[UnloadNumber];
            do
            {
                int count;
                lock (this.syncObject)
                {// Get flake/himo type array.
                    this.goshujin.UnloadQueueChain.TryPeek(out var himo);
                    for (count = 0; himo != null && count < UnloadNumber; count++)
                    {
                        array[count].Flake = himo.Flake;
                        array[count].HimoType = himo.HimoType;
                        himo = himo.UnloadQueueLink.Next;
                    }
                }

                for (var i = 0; i < count; i++)
                {
                    array[i].Flake.Unload(array[i].HimoType);
                }
            }
            while (Volatile.Read(ref this.totalSize) > limit);
        }

        internal void Clear()
        {
            lock (this.syncObject)
            {
                this.goshujin.Clear();
            }
        }

        private object syncObject = new();
        private long totalSize; // lock(this.syncObject)
        private Himo.GoshujinClass goshujin = new(); // lock(this.syncObject)
        private HimoTaskCore? taskCore;
    }
}
