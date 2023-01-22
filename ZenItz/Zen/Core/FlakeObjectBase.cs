// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class FlakeObjectBase<TIdentifier>
{
    public enum FlakeObjectOperation
    {
        Set, // Set value
        Get, // Get value
        Remove,
    }

    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public FlakeObjectBase(Flake<TIdentifier> flake, FlakeObjectGoshujin<TIdentifier> goshujin)
    {
        this.Flake = flake;
        this.FlakeObjectGoshujin = goshujin;
    }

    internal virtual void Save(bool unload)
    {
    }

    internal void UpdateQueue(FlakeObjectOperation operation, (bool Changed, int MemoryDifference) t)
    {// Update queue link.
        if (t.Changed)
        {
            this.IsSaved = false;
        }

        lock (this.FlakeObjectGoshujin.Goshujin)
        {
            if (operation == FlakeObjectOperation.Remove)
            {// Remove
                this.Goshujin = null;
            }
            else
            {// Get or Set
                if (this.Goshujin == null)
                {// New
                    this.Goshujin = this.FlakeObjectGoshujin.Goshujin;
                }
                else
                {// Update
                    if (operation == FlakeObjectOperation.Get)
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
                    }
                }
            }

            this.FlakeObjectGoshujin.TotalSize += t.MemoryDifference;
            while (this.FlakeObjectGoshujin.TotalSize > this.Flake.Zen.Options.MemorySizeLimit)
            {// Unload
                var h = this.FlakeObjectGoshujin.Goshujin.UnloadQueueChain.Peek();
                h.Save(true);
            }
        }
    }

    internal void RemoveQueue(int memoryDifference)
    {// Remove link
        lock (this.FlakeObjectGoshujin.Goshujin)
        {
            this.FlakeObjectGoshujin.TotalSize += memoryDifference;
            this.Goshujin = null;
        }
    }

    public Flake<TIdentifier> Flake { get; }

    public FlakeObjectGoshujin<TIdentifier> FlakeObjectGoshujin { get; }

    public bool IsSaved { get; protected set; }
}
