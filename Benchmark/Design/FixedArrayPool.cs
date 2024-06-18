// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Lp;

/// <summary>
/// A thread-safe pool of fixed-length (1 kbytes or more) byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// </summary>
public class FixedArrayPool
{
    /// <summary>
    /// An owner class of a byte array (one owner for each byte array).<br/>
    /// <see cref="Owner"/> has a reference count, and when it reaches zero, it returns the byte array to the pool.
    /// </summary>
    public class Owner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Owner"/> class from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="FixedArrayPool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="FixedArrayPool"/>).</param>
        public Owner(byte[] byteArray)
        {
            this.Pool = null;
            this.ByteArray = byteArray;
            this.SetCount1();
        }

        internal Owner(FixedArrayPool pool)
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

        public MemoryOwner IncrementAndShare(int start, int length)
        {
            Interlocked.Increment(ref this.count);
            return new MemoryOwner(this, start, length);
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
            if (count == 0 && this.Pool != null)
            {
                if (this.Pool.MaxPool == 0 || this.Pool.queue.Count <= this.Pool.MaxPool)
                {
                    this.Pool.queue.Enqueue(this);
                }
            }

            return null;
        }

        public void Dispose() => this.Return();

        public MemoryOwner AsMemory() => new MemoryOwner(this);

        public MemoryOwner AsMemory(int start, int length) => new MemoryOwner(this, start, length);

        internal void SetCount1() => Volatile.Write(ref this.count, 1);

        /// <summary>
        /// Gets a <see cref="FixedArrayPool"/> instance.
        /// </summary>
        public FixedArrayPool? Pool { get; }

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

    public readonly struct MemoryOwner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryOwner"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="FixedArrayPool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="FixedArrayPool"/>).</param>
        public MemoryOwner(byte[] byteArray)
        {
            this.Owner = new(byteArray);
            this.Memory = byteArray.AsMemory();
        }

        internal MemoryOwner(Owner owner)
        {
            this.Owner = owner;
            this.Memory = owner.ByteArray.AsMemory();
        }

        internal MemoryOwner(Owner owner, int start, int length)
        {
            this.Owner = owner;
            this.Memory = owner.ByteArray.AsMemory(start, length);
        }

        internal MemoryOwner(Owner owner, Memory<byte> memory)
        {
            this.Owner = owner;
            this.Memory = memory;
        }

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Owner"/> instance (<see langword="this"/>).</returns>
        public MemoryOwner IncrementAndShare()
        {
            if (this.Owner == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.Owner.IncrementAndShare(), this.Memory);
        }

        public MemoryOwner IncrementAndShare(int start, int length)
        {
            if (this.Owner == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.Owner.IncrementAndShare(), start, length);
        }

        /// <summary>
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="MemoryOwner"/>.</returns>
        public MemoryOwner Slice(int start)
            => new(this.Owner!, this.Memory.Slice(start));

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="MemoryOwner"/>.</returns>
        public MemoryOwner Slice(int start, int length)
            => new(this.Owner!, this.Memory.Slice(start, length));

        public MemoryOwner Return()
        {
            this.Owner?.Return();
            return default;
        }

        public void Dispose() => this.Return();

        public readonly Owner? Owner;
        public readonly Memory<byte> Memory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArrayPool"/> class.<br/>
    /// </summary>
    /// <param name="arrayLength">The length of fixed-length byte array.</param>
    /// <param name="maxPool">The maximum number of pooled arrays (0 for unlimited).</param>
    public FixedArrayPool(int arrayLength, int maxPool = 0)
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
            return new Owner(this);
        }

        owner.SetCount1();
        return owner;
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
