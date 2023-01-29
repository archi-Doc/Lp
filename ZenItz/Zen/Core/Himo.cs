// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
            [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
            public Himo(HimoGoshujinClass goshujin, Flake flake)
            {
                this.himoGoshujin = goshujin;
                this.flake = flake;
            }

            internal void Update(int memoryDifference)
            {
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
                    if (this.himoGoshujin.totalSize > this.himoGoshujin.Zen.Options.MemorySizeLimit)
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
            {
                lock (this.himoGoshujin.syncObject)
                {
                    this.Goshujin = null;
                    this.himoGoshujin.totalSize += memoryDifference;
                }
            }

            internal Flake Flake => this.flake;

            private HimoGoshujinClass himoGoshujin;
            private Flake flake;
        }

        public HimoGoshujinClass(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
            this.taskCore = new(ThreadCore.Root, this);
        }

        public Zen<TIdentifier> Zen { get; }

        internal void Unload()
        {
            var limit = this.Zen.Options.MemorySizeLimit > MemoryMargin ? (this.Zen.Options.MemorySizeLimit - MemoryMargin) : this.Zen.Options.MemorySizeLimit;
            while (Volatile.Read(ref this.totalSize) > limit)
            {
                var count = 0;
                var flakes = new Flake[UnloadNumber];

                lock (this.syncObject)
                {// Get flake array.
                    this.goshujin.UnloadQueueChain.TryPeek(out var himo);
                    for (count = 0; himo != null && count < UnloadNumber; count++)
                    {
                        flakes[count] = himo.Flake;
                        himo = himo.UnloadQueueLink.Next;
                    }
                }

                foreach (var x in flakes)
                {
                    x.Unload();
                }
            }
        }

        internal void ClearInternal()
        {
            lock (this.syncObject)
            {
                this.goshujin.Clear();
            }
        }

        private object syncObject = new();

        private long totalSize; // lock(this.syncObject)

        private Himo.GoshujinClass goshujin = new(); // lock(this.syncObject)

        private HimoTaskCore taskCore;
    }
}
