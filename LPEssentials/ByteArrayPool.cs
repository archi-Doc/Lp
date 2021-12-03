// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// A fast and thread-safe pool of fixed-length (1 kbytes or more) byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// </summary>
public class ByteArrayPool
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnAndSetNull(ref Owner? owner)
    {
        if (owner != null)
        {
            owner = owner.Return();
        }
    }

    /// <summary>
    /// An owner class of a byte array (one owner for each byte array).<br/>
    /// <see cref="Owner"/> has a reference count, and when it reaches zero, it returns the byte array to the pool.
    /// </summary>
    public class Owner
    {
        internal Owner(ByteArrayPool pool)
        {
            this.Pool = pool;
            this.ByteArray = new byte[pool.ArrayLength];
            this.SetCount1();
        }

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Owner"/> instance (<see langword="this"/>).</returns>
        public Owner IncrementAndShare()
        {
            Interlocked.Increment(ref this.count);
            return this;
        }

        /// <summary>
        /// Decrement the reference count. When it reaches zero, it returns the byte array to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="null"></see>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Owner? Return()
        {
            var count = Interlocked.Decrement(ref this.count);
            if (count == 0)
            {
                this.Pool.Return(this);
            }

            return null;
        }

        internal void SetCount1() => Volatile.Write(ref this.count, 1);

        /// <summary>
        /// Gets a <see cref="ByteArrayPool"/> instance.
        /// </summary>
        public ByteArrayPool Pool { get; }

        /// <summary>
        /// Gets a fixed-length byte array.
        /// </summary>
        public byte[] ByteArray { get; }

        /// <summary>
        /// Gets the reference count of the owner.
        /// </summary>
        public int Count => Volatile.Read(ref this.count);

        private int count;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayPool"/> class.<br/>
    /// </summary>
    /// <param name="arrayLength">The length of fixed-length byte array.</param>
    /// <param name="maxPool">The maximum number of pooled arrays (0 for unlimited).</param>
    public ByteArrayPool(int arrayLength, int maxPool = 0)
    {
        this.ArrayLength = arrayLength;
        this.MaxPool = maxPool >= 0 ? maxPool : 0;
    }

    /// <summary>
    /// Gets a fixed-length byte array from the pool or create a new byte array if not available.<br/>
    /// </summary>
    /// <returns>A fixed-length byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Owner Rent()
    {
        Owner? owner;
        if (!this.queue.TryDequeue(out owner))
        {// Allocate a new byte array.
            owner = new Owner(this);
        }

        owner.SetCount1();
        return owner;
    }

    /// <summary>
    /// Returns a byte array to the pool.<br/>
    /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
    /// </summary>
    /// <param name="owner">An owner of the byte array to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Return(Owner owner)
    {
        if (owner.ByteArray.Length == this.ArrayLength)
        {
            if (this.MaxPool == 0 || this.queue.Count <= this.MaxPool)
            {
                this.queue.Enqueue(owner);
            }
        }
    }

    /// <summary>
    /// Gets the length of fixed-length byte array.
    /// </summary>
    public int ArrayLength { get; }

    /// <summary>
    /// Gets the maximum number of pooled arrays (0 for unlimited).
    /// </summary>
    public int MaxPool { get; }

    private ConcurrentQueue<Owner> queue = new();
}
