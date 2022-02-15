// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class SnowObject
{
    public enum SnowObjectState
    {
        File,
        Memory,
        Object,
    }

    public enum SnowObjectOperation
    {
        Set, // Set value
        Get, // Get value
    }

    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public SnowObject(Flake flake, SnowObjectGoshujin snowObjectControl)
    {
        this.Flake = flake;
        this.SnowObjectGoshujin = snowObjectControl;
    }

    internal virtual void Save(bool unload)
    {
    }

    internal void UpdateQueue(SnowObjectOperation operation, int diff)
    {// Update queue link.
        lock (this.SnowObjectGoshujin.Goshujin)
        {
            if (this.Goshujin == null)
            {// New
                this.Goshujin = this.SnowObjectGoshujin.Goshujin;
            }
            else
            {// Update
                if (operation == SnowObjectOperation.Get)
                {// Get
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                }
                else
                {// Set and other
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                    this.Goshujin.SaveQueueChain.Remove(this);
                    this.Goshujin.SaveQueueChain.Enqueue(this);
                }
            }

            // this.SnowObjectGoshujin.Update(diff);
            this.SnowObjectGoshujin.TotalSize += diff;
            while (this.SnowObjectGoshujin.TotalSize > Zen.DefaultPrimarySizeLimit)
            {// Unload
                var h = this.SnowObjectGoshujin.Goshujin.UnloadQueueChain.Peek();
                h.Save(true);
            }
        }
    }

    internal void RemoveQueue(int diff)
    {// Remove link
        lock (this.SnowObjectGoshujin.Goshujin)
        {
            this.SnowObjectGoshujin.TotalSize += diff;
            this.Goshujin = null;
        }
    }

    public Flake Flake { get; }

    public SnowObjectGoshujin SnowObjectGoshujin { get; }

    public SnowObjectState State { get; protected set; }
}
