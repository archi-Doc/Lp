﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace ZenItz;

public class HimoControl
{
    public const long DefaultSizeLimit = 100_000_000;

    public HimoControl(Zen zen)
    {
        this.zen = zen;
        this.pool = new ByteArrayPool(BlockService.MaxBlockSize, 100);
        this.sizeLimit = DefaultSizeLimit;
    }

    internal Himo Create(in Identifier primaryId, in Identifier secondaryId, ReadOnlySpan<byte> data)
    {
        Himo? himo;
        List<HimoIdentifier>? remove = null;
        lock (this.goshujin)
        {
            if (!this.freeHimo.TryDequeue(out himo))
            {
                himo = new Himo();
            }

            himo.MemoryOwner = this.pool.Rent(data.Length).ToMemoryOwner(0, data.Length);
            data.CopyTo(himo.MemoryOwner.Memory.Span);
            himo.Identifier = new HimoIdentifier(primaryId, true);

            himo.Goshujin = this.goshujin;
            this.totalSize += data.Length;

            while (this.totalSize > this.sizeLimit)
            {// Unload
                var h = this.goshujin.UnloadQueueChain.Dequeue();
                remove ??= new();
                remove.Add(h.Identifier);
                this.totalSize -= h.MemoryOwner.Memory.Length;
            }
        }

        if (remove != null)
        {
            foreach (var x in remove)
            {// Unload
                // this.zen.Unload(in x.PrimaryId, in x.SecondaryId);
                // this.
                // this.Release(in x.PrimaryId, in x.SecondaryId);
            }
        }

        return himo;
    }

    /*internal void Release(in Identifier primaryId, in Identifier secondaryId)
    {
        lock (this.goshujin)
        {
            if (this.goshujin.IdentifierChain.TryGetValue(new PrimarySecondaryIdentifier(primaryId, secondaryId), out var himo))
            {
                this.totalSize -= himo.MemoryOwner.Memory.Length;
                himo.Clear();
                this.freeHimo.Enqueue(himo);
            }
        }
    }*/

    private Zen zen;
    private ByteArrayPool pool;
    private Himo.GoshujinClass goshujin = new();
    private long sizeLimit;
    private long totalSize;
    private Queue<Himo> freeHimo = new();
}
