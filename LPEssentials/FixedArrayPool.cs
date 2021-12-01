// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// A fast and thread-safe pool of fixed-length byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// </summary>
public class FixedArrayPool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArrayPool"/> class.<br/>
    /// </summary>
    /// <param name="arrayLength">The length of fixed-length byte array.</param>
    /// <param name="maxArrays">The maximum number of arrays in the pool (0 for unlimited).</param>
    public FixedArrayPool(int arrayLength, int maxArrays = 0)
    {
        this.ArrayLength = arrayLength;
        this.MaxArrays = maxArrays >= 0 ? maxArrays : 0;
    }

    /// <summary>
    /// Gets a fixed-length byte array from the pool or create a new byte array if not available.<br/>
    /// </summary>
    /// <returns>A fixed-length byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Rent()
    {
        byte[]? array;
        if (!this.queue.TryDequeue(out array))
        {// Allocate a new byte array.
            array = new byte[this.ArrayLength];
        }

        return array;
    }

    /// <summary>
    /// Returns a byte array to the pool.
    /// </summary>
    /// <param name="array">A buffer to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(byte[] array)
    {
        if (array.Length == this.ArrayLength)
        {
            if (this.MaxArrays == 0 || this.queue.Count <= this.MaxArrays)
            {
                this.queue.Enqueue(array);
            }
        }
    }

    public int ArrayLength { get; }

    public int MaxArrays { get; }

    private ConcurrentQueue<byte[]> queue = new();
}
