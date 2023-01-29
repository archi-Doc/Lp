// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal class HimoGoshujinClass
    {
        public const int UnloadInterval = 100; // 100 ms
        public const int UnloadNumber = 10;
        public const long MemoryMargin = 1024 * 1024 * 100; // 100 MB

        public HimoGoshujinClass(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
            this.taskCore = new(ThreadCore.Root, this);
        }

        public Zen<TIdentifier> Zen { get; }

        public Himo.GoshujinClass Goshujin => this.goshujin;

        internal void Unload()
        {
            var limit = this.Zen.Options.MemorySizeLimit > MemoryMargin ? (this.Zen.Options.MemorySizeLimit - MemoryMargin) : this.Zen.Options.MemorySizeLimit;
            while (this.TotalSize > limit)
            {
                var count = 0;
                var flakes = new Flake[UnloadNumber];

                lock (this.Goshujin)
                {// Get flake array.
                    this.Goshujin.UnloadQueueChain.TryPeek(out var himo);
                    for (count = 0; himo != null && count < UnloadNumber; count++)
                    {
                        flakes[count] = himo.Flake;
                    }
                }

                this.Zen.Root.

                while (count < UnloadNumber && this.Goshujin.UnloadQueueChain.TryDequeue(out var himo))
                {

                }

                // Unload
                var h = worker.himoGoshujin.Goshujin.UnloadQueueChain.Peek();
                h.Save(true);
            }
        }

        internal long TotalSize;

        private Himo.GoshujinClass goshujin = new();

        private HimoTaskCore taskCore;
    }
}
