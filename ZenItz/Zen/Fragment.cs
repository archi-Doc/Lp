// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class Fragment
{
    public enum FragmentState
    {
        NotSaved, // Active and not saved
        Saved, // Active and saved
    }

    public enum FragmentOperation
    {
        Set, // Set value
        Get, // Get value
    }

    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public Fragment(Flake flake)
    {
        this.Flake = flake;
    }

    internal void UpdateQueue(FragmentOperation operation)
    {// Update queue link.
        List<FragmentIdentifier>? remove = null;
        lock (this.Flake.Zen.FragmentGoshujin)
        {
            if (this.Goshujin == null)
            {// New
                this.Goshujin = this.Flake.Zen.FragmentGoshujin;

                while (this.totalSize > this.sizeLimit)
                {// Unload
                    var h = this.goshujin.UnloadQueueChain.Dequeue();
                    remove ??= new();
                    remove.Add(h.Identifier);
                    this.totalSize -= h.MemoryOwner.Memory.Length;
                }
            }
            else
            {// Update
                if (operation == FragmentOperation.Get)
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
        lock (this.Flake.Zen.FragmentGoshujin)
        {
            this.Goshujin = null;
        }
    }

    public Flake Flake { get; }

    public FragmentState State { get; protected set; }
}
