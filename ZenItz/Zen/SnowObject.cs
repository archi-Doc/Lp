// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class SnowObject
{
    public enum SnowObjectState
    {
        NotSaved, // Active and not saved
        Saved, // Active and saved
    }

    public enum SnowObjectOperation
    {
        Set, // Set value
        Get, // Get value
    }

    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public SnowObject(SnowObjectGoshujin snowObjectControl)
    {
        this.SnowObjectGoshujin = snowObjectControl;
    }

    internal void UpdateQueue(SnowObjectOperation operation)
    {// Update queue link.
        List<FragmentIdentifier>? remove = null;
        lock (this.SnowObjectGoshujin.Goshujin)
        {
            if (this.Goshujin == null)
            {// New
                this.Goshujin = this.SnowObjectGoshujin.Goshujin;

                /*while (this.totalSize > this.sizeLimit)
                {// Unload
                    var h = this.goshujin.UnloadQueueChain.Dequeue();
                    remove ??= new();
                    remove.Add(h.Identifier);
                    this.totalSize -= h.MemoryOwner.Memory.Length;
                }*/
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
        }
    }

    internal void RemoveQueue()
    {// Remove link
        lock (this.SnowObjectGoshujin.Goshujin)
        {
            this.Goshujin = null;
        }
    }

    public SnowObjectGoshujin SnowObjectGoshujin { get; }

    public SnowObjectState State { get; protected set; }
}
