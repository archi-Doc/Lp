// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// A fast and thread-safe pool of fixed-length (1 kbytes or more) byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// </summary>
public class FixedArrayPoolObsolete
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArrayPoolObsolete"/> class.<br/>
    /// </summary>
    /// <param name="arrayLength">The length of fixed-length byte array.</param>
    /// <param name="maxPool">The maximum number of pooled arrays (0 for unlimited).</param>
    public FixedArrayPoolObsolete(int arrayLength, int maxPool = 0)
    {
        this.ArrayLength = arrayLength;
        this.MaxPool = maxPool >= 0 ? maxPool : 0;
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
    /// Returns a byte array to the pool.<br/>
    /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
    /// </summary>
    /// <param name="array">A buffer to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(byte[] array)
    {
        if (array.Length != this.ArrayLength)
        {
            throw new InvalidDataException("The length of the byte array does not match the specified length.");
        }

        if (this.MaxPool == 0 || this.queue.Count <= this.MaxPool)
        {
            this.queue.Enqueue(array);
        }
    }

    public int ArrayLength { get; }

    public int MaxPool { get; }

    private ConcurrentQueue<byte[]> queue = new();
}
