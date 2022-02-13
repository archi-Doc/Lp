// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

[ValueLinkObject]
internal partial class Himo
{
    [Link(Name = "UnloadQueue", Type = ChainType.QueueList)]
    [Link(Name = "SaveQueue", Type = ChainType.QueueList)]
    public Himo()
    {
    }

    internal void Clear()
    {
        this.MemoryOwner.Return();
        this.Goshujin = null;
    }

    // [Link(Type = ChainType.Unordered)]
    internal HimoIdentifier Identifier;

    internal ByteArrayPool.MemoryOwner MemoryOwner;
}
