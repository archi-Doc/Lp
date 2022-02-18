// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class FlakeObjectBase
{
    public enum FlakeObjectState
    {
        File,
        Memory,
        Object,
    }

    public enum FlakeObjectOperation
    {
        Set, // Set value
        Get, // Get value
    }

    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public FlakeObjectBase(Flake flake, FlakeObjectGoshujin snowObjectControl)
    {
        this.Flake = flake;
        this.FlakeObjectGoshujin = snowObjectControl;
    }

    internal virtual void Save(bool unload)
    {
    }

    internal void UpdateQueue(FlakeObjectOperation operation, int memoryDifference)
    {// Update queue link.
        lock (this.FlakeObjectGoshujin.Goshujin)
        {
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
                {// Set and other
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                    this.Goshujin.SaveQueueChain.Remove(this);
                    this.Goshujin.SaveQueueChain.Enqueue(this);
                }
            }

            // this.SnowObjectGoshujin.Update(diff);
            this.FlakeObjectGoshujin.TotalSize += memoryDifference;
            while (this.FlakeObjectGoshujin.TotalSize > Zen.DefaultPrimarySizeLimit)
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

    public Flake Flake { get; }

    public FlakeObjectGoshujin FlakeObjectGoshujin { get; }

    public FlakeObjectState State { get; protected set; }
}
