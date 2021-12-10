// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// A thread-safe pool of byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// </summary>
public class UnifiedArrayPool
{
    private const int LowerBoundBits = 3;

    /// <summary>
    /// An owner class of a byte array (one owner instance for each byte array).<br/>
    /// <see cref="Owner"/> has a reference count, and when it reaches zero, it returns the byte array to the pool.
    /// </summary>
    public class Owner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Owner"/> class from a byte array.<br/>
        /// This is a feature for compatibility with conventional memory management (e.g new byte[]), <br/>
        /// The byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (allocated with 'new').</param>
        public Owner(byte[] byteArray)
        {
            this.bucket = null;
            this.ByteArray = byteArray;
            this.SetCount1();
        }

        internal Owner(UnifiedArrayPool.Bucket bucket)
        {
            this.bucket = bucket;
            this.ByteArray = new byte[bucket.ArrayLength];
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
            if (count == 0 && this.bucket != null)
            {
                if (this.bucket.MaxPool == 0 || this.bucket.Queue.Count < this.bucket.MaxPool)
                {
                    this.bucket.Queue.Enqueue(this);
                }
            }

            return null;
        }

        public void Dispose() => this.Return();

        public MemoryOwner ToMemoryOwner() => new MemoryOwner(this);

        public MemoryOwner ToMemoryOwner(int start, int length) => new MemoryOwner(this, start, length);

        internal void SetCount1() => Volatile.Write(ref this.count, 1);

        /// <summary>
        /// Gets a fixed-length byte array.
        /// </summary>
        public byte[] ByteArray { get; }

        /// <summary>
        /// Gets the reference count of the owner.
        /// </summary>
        public int Count => Volatile.Read(ref this.count);

        private UnifiedArrayPool.Bucket? bucket;
        private int count;
    }

    public readonly struct MemoryOwner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryOwner"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="UnifiedArrayPool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="UnifiedArrayPool"/>).</param>
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

    internal sealed class Bucket
    {
        public Bucket(UnifiedArrayPool pool, int arrayLength, int maxPool)
        {
            this.pool = pool;
            this.ArrayLength = arrayLength;
            this.MaxPool = maxPool;
        }

        public int ArrayLength { get; }

        public int MaxPool { get; }

#pragma warning disable SA1401 // Fields should be private
        internal ConcurrentQueue<Owner> Queue = new();
#pragma warning restore SA1401 // Fields should be private

        private UnifiedArrayPool pool;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedArrayPool"/> class.<br/>
    /// </summary>
    /// <param name="maxLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="maxPool">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    public UnifiedArrayPool(int maxLength, int maxPool = 100)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        var leadingZero = BitOperations.LeadingZeroCount((uint)maxLength);
        var lowerBound = 32 - LowerBoundBits;
        if (leadingZero > lowerBound)
        {
            leadingZero = lowerBound;
        }
        else if (leadingZero < 1)
        {
            leadingZero = 1;
        }

        this.MaxLength = 1 << (32 - leadingZero);
        this.MaxPool = maxPool >= 0 ? maxPool : 0;

        this.buckets = new Bucket[33];
        for (var i = 0; i <= 32; i++)
        {
            if (i < leadingZero)
            {
                this.buckets[i] = null;
            }
            else if (i > lowerBound)
            {
                this.buckets[i] = this.buckets[lowerBound];
            }
            else
            {
                this.buckets[i] = new(this, 1 << (32 - i), this.MaxPool);
            }
        }
    }

    /// <summary>
    /// Gets a fixed-length byte array from the pool or create a new byte array if not available.<br/>
    /// </summary>
    /// <param name="minimumLength">The minimum length of the array.</param>
    /// <returns>A fixed-length byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Owner Rent(int minimumLength)
    {
        var bucket = this.buckets[BitOperations.LeadingZeroCount((uint)minimumLength)];
        if (bucket == null)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }

        Owner? owner;
        if (!bucket.Queue.TryDequeue(out owner))
        {// Allocate a new byte array.
            return new Owner(bucket);
        }

        owner.SetCount1();
        return owner;
    }

    /// <summary>
    /// Gets the maximum length of a byte array instance that may be stored in the pool.
    /// </summary>
    public int MaxLength { get; }

    /// <summary>
    /// Gets the maximum number of array instances that may be stored in each bucket in the pool.
    /// </summary>
    public int MaxPool { get; }

    private Bucket?[] buckets;
}
