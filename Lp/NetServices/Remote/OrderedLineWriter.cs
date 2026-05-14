// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Lp.NetServices.Remote;

internal sealed class OrderedLineWriter
{
    private struct Entry
    {
        public int Line;
        public string? Message;
        public ConsoleColor Color;
    }

    #region FieldAndProperty

    private readonly Entry[] buffer;
    private readonly int mask;
    private readonly Action<string?, ConsoleColor> writeDelegate;
    private int nextLine;

    public int NextLine => this.nextLine;

    public int Capacity => this.buffer.Length;

    #endregion

    public OrderedLineWriter(int bufferCapacity, Action<string?, ConsoleColor> writeDelegate)
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

    public void Add(int line, string? message, ConsoleColor color)
    {
        var next = this.nextLine;

        // A line older than NextLine arrived later.
        // Since ordering may be broken in overflow cases, output it immediately.
        if (line < next)
        {
            this.writeDelegate(message, color);
            return;
        }

        // Fast path: the expected line arrived.
        if (line == next)
        {
            this.WriteAndAdvance(message, color);
            this.FlushReadyLines();
            return;
        }

        var distance = line - next;

        // Normal path: the line fits into the reorder buffer.
        if ((uint)distance < (uint)this.buffer.Length)
        {
            this.StoreOrWriteDuplicate(line, message, color);
            return;
        }

        // Overflow path:
        // The incoming line is too far ahead.
        // First, output all currently available contiguous lines.
        this.FlushReadyLines();

        next = this.nextLine;
        if (line < next)
        {
            this.writeDelegate(message, color);
            return;
        }
        else if (line == next)
        {
            this.WriteAndAdvance(message, color);
            this.FlushReadyLines();
            return;
        }

        distance = line - next;
        if ((uint)distance < (uint)this.buffer.Length)
        {
            this.StoreOrWriteDuplicate(line, message, color);
            return;
        }

        // Still too far ahead.
        // Give up waiting for missing lines and output buffered messages as much as possible.
        this.ForceFlushBufferedLines();

        // Output the incoming message even though ordering may be broken.
        this.writeDelegate(message, color);

        // Continue from the next expected line after the incoming one.
        this.nextLine = line + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAndAdvance(string? message, ConsoleColor color)
    {
        this.writeDelegate(message, color);
        this.nextLine++;
    }

    private void StoreOrWriteDuplicate(int line, string? message, ConsoleColor color)
    {
        ref var entry = ref this.buffer[line & this.mask];
        if (entry.Line == -1)
        {
            entry.Line = line;
            entry.Message = message;
            entry.Color = color;
            return;
        }

        // this.writeDelegate(message, color);
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
            var color = entry.Color;
            entry.Line = -1;
            entry.Message = null;

            this.WriteAndAdvance(message, color);
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

            this.writeDelegate(entry.Message, entry.Color);

            entry.Line = -1;
            entry.Message = null;

            if (bestLine >= this.nextLine)
            {
                this.nextLine = bestLine + 1;
            }
        }
    }
}
