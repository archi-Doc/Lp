// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Lp.NetServices.Remote;

internal sealed class OrderedLineWriter
{
    private readonly Entry[] buffer;
    private readonly int mask;
    private readonly Action<string?> writeDelegate;
    private int nextLine;

    private struct Entry
    {
        public int Line;
        public string? Message;
        public LogLevel 
    }

    public OrderedLineWriter(int bufferCapacity, Action<string?> writeDelegate)
    {
        var bufferSizePowerOfTwo = CollectionHelper.CalculatePowerOfTwoCapacity(bufferCapacity);

        this.buffer = new Entry[bufferSizePowerOfTwo];
        this.mask = bufferSizePowerOfTwo - 1;
        this.writeDelegate = writeDelegate;
        this.nextLine = 0;

        for (var i = 0; i < this.buffer.Length; i++)
        {
            this.buffer[i].Line = -1;
        }
    }

    public int NextLine => this.nextLine;

    public int Capacity => this.buffer.Length;

    public void Add(int line, string? message)
    {
        var next = this.nextLine;

        // A line older than NextLine arrived later.
        // Since ordering may be broken in overflow cases, output it immediately.
        if (line < next)
        {
            this.writeDelegate(message);
            return;
        }

        // Fast path: the expected line arrived.
        if (line == next)
        {
            this.WriteAndAdvance(message);
            this.FlushReadyLines();
            return;
        }

        var distance = line - next;

        // Normal path: the line fits into the reorder buffer.
        if ((uint)distance < (uint)this.buffer.Length)
        {
            this.StoreOrWriteDuplicate(line, message);
            return;
        }

        // Overflow path:
        // The incoming line is too far ahead.
        // First, output all currently available contiguous lines.
        this.FlushReadyLines();

        next = this.nextLine;
        if (line < next)
        {
            this.writeDelegate(message);
            return;
        }
        else if (line == next)
        {
            this.WriteAndAdvance(message);
            this.FlushReadyLines();
            return;
        }

        distance = line - next;
        if ((uint)distance < (uint)this.buffer.Length)
        {
            this.StoreOrWriteDuplicate(line, message);
            return;
        }

        // Still too far ahead.
        // Give up waiting for missing lines and output buffered messages as much as possible.
        this.ForceFlushBufferedLines();

        // Output the incoming message even though ordering may be broken.
        this.writeDelegate(message);

        // Continue from the next expected line after the incoming one.
        this.nextLine = line + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAndAdvance(string? message)
    {
        this.writeDelegate(message);
        this.nextLine++;
    }

    private void StoreOrWriteDuplicate(int line, string? message)
    {
        ref var entry = ref this.buffer[line & this.mask];

        if (entry.Line == -1)
        {
            entry.Line = line;
            entry.Message = message;
            return;
        }

        // Same line or unexpected slot collision.
        // Since the requirement is to output as much as possible,
        // output the incoming message instead of throwing.
        this.writeDelegate(message);
    }

    private void FlushReadyLines()
    {
        while (true)
        {
            ref var entry = ref this.buffer[this.nextLine & this.mask];

            if (entry.Line != this.nextLine)
            {
                return;
            }

            var message = entry.Message;
            entry.Line = -1;
            entry.Message = null;

            this.WriteAndAdvance(message);
        }
    }

    private void ForceFlushBufferedLines()
    {
        while (true)
        {
            var bestIndex = -1;
            var bestLine = int.MaxValue;

            for (var i = 0; i < this.buffer.Length; i++)
            {
                var line = this.buffer[i].Line;

                if (line >= 0 && line < bestLine)
                {
                    bestLine = line;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return;
            }

            ref var entry = ref this.buffer[bestIndex];

            this.writeDelegate(entry.Message);

            entry.Line = -1;
            entry.Message = null;

            if (bestLine >= this.nextLine)
            {
                this.nextLine = bestLine + 1;
            }
        }
    }
}
