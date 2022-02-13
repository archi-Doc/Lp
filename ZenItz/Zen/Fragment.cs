// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class Fragment
{// by Yamamoto.
    public enum FragmentState
    {
        NotLoaded, // Not loaded
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
    {
        lock (this.Flake.Zen.FragmentGoshujin)
        {
            if (this.Goshujin == null)
            {// New
                this.Goshujin = this.Flake.Zen.FragmentGoshujin;
            }
            else
            {// Update
                if (operation == FragmentOperation.Get)
                {
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                }
                else
                {
                    this.Goshujin.UnloadQueueChain.Remove(this);
                    this.Goshujin.UnloadQueueChain.Enqueue(this);
                    this.Goshujin.SaveQueueChain.Remove(this);
                    this.Goshujin.SaveQueueChain.Enqueue(this);
                }
            }
        }
    }

    public Flake Flake { get; }

    public FragmentState State { get; private set; }
}
