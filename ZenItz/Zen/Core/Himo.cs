// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    [ValueLinkObject]
    internal partial class Himo
    {
        public enum HimoOperation
        {
            Set, // Set value
            Get, // Get value
            Remove,
        }

        [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
        // [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
        public Himo(Flake flake, HimoGoshujinClass goshujin)
        {
            this.Flake = flake;
            this.HimoGoshujin = goshujin;
        }

        internal virtual void Save(bool unload)
        {
        }

        internal void UpdateQueue(HimoOperation operation, (bool Changed, int MemoryDifference) t)
        {// Update queue link.
            if (t.Changed)
            {
                this.IsSaved = false;
            }

            lock (this.HimoGoshujin.Goshujin)
            {
                if (operation == HimoOperation.Remove)
                {// Remove
                    this.Goshujin = null;
                }
                else
                {// Get or Set
                    if (this.Goshujin == null)
                    {// New
                        this.Goshujin = this.HimoGoshujin.Goshujin;
                    }
                    else
                    {// Update
                        this.Goshujin.UnloadQueueChain.Remove(this);
                        this.Goshujin.UnloadQueueChain.Enqueue(this);

                        /* if (operation == HimoOperation.Get)
                         {// Get
                             this.Goshujin.UnloadQueueChain.Remove(this);
                             this.Goshujin.UnloadQueueChain.Enqueue(this);
                         }
                         else
                         {// Set
                             this.Goshujin.UnloadQueueChain.Remove(this);
                             this.Goshujin.UnloadQueueChain.Enqueue(this);
                             this.Goshujin.SaveQueueChain.Remove(this);
                             this.Goshujin.SaveQueueChain.Enqueue(this);
                         }*/
                    }
                }

                this.HimoGoshujin.TotalSize += t.MemoryDifference;
                while (this.HimoGoshujin.TotalSize > this.Flake.Zen.Options.MemorySizeLimit)
                {// Unload
                    var h = this.HimoGoshujin.Goshujin.UnloadQueueChain.Peek();
                    h.Save(true);
                }
            }
        }

        internal void RemoveQueue(int memoryDifference)
        {// Remove link
            lock (this.HimoGoshujin.Goshujin)
            {
                this.HimoGoshujin.TotalSize += memoryDifference;
                this.Goshujin = null;
            }
        }

        public Flake Flake { get; }

        public HimoGoshujinClass HimoGoshujin { get; }

        public bool IsSaved { get; protected set; }
    }
}
