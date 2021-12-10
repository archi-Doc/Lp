// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// A thread-safe pool of byte arrays (uses <see cref="ArrayPool{T}"/>).<br/>
/// </summary>
public class ByteArrayPool
{
    /// <summary>
    /// An owner struct of a byte array (one owner for each byte array).<br/>
    /// </summary>
    public readonly struct Owner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Owner"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteArrayPool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteArrayPool"/>).</param>
        public Owner(byte[] byteArray)
        {
            this.pool = null;
            this.ByteArray = byteArray;
        }

        internal Owner(ByteArrayPool pool, byte[] byteArray)
        {
            this.pool = pool;
            this.ByteArray = byteArray;
        }

        /// <summary>
        /// Decrement the reference count. When it reaches zero, it returns the byte array to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="null"></see>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Owner Return()
        {
            this.pool?.arrayPool.Return(this.ByteArray);
            return default;
        }

        public void Dispose() => this.Return();

        public MemoryOwner ToMemoryOwner() => new MemoryOwner(this);

        public MemoryOwner ToMemoryOwner(int start, int length) => new MemoryOwner(this, start, length);

        /// <summary>
        /// Gets a byte array.
        /// </summary>
        public readonly byte[] ByteArray;

        private readonly ByteArrayPool? pool;
    }

    public readonly struct MemoryOwner : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryOwner"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteArrayPool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteArrayPool"/>).</param>
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
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="MemoryOwner"/>.</returns>
        public MemoryOwner Slice(int start)
            => new(this.Owner, this.Memory.Slice(start));

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="MemoryOwner"/>.</returns>
        public MemoryOwner Slice(int start, int length)
            => new(this.Owner, this.Memory.Slice(start, length));

        public MemoryOwner Return()
        {
            this.Owner.Return();
            return default;
        }

        public void Dispose() => this.Return();

        public readonly Owner Owner;
        public readonly Memory<byte> Memory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayPool"/> class.<br/>
    /// </summary>
    /// <param name="maxLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="maxPool">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    public ByteArrayPool(int maxLength, int maxPool = 100)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        this.MaxLength = maxLength;
        this.MaxPool = maxPool >= 0 ? maxPool : 0;
        this.arrayPool = ArrayPool<byte>.Create(this.MaxLength, this.MaxPool);
    }

    /// <summary>
    /// Gets a fixed-length byte array from the pool or create a new byte array if not available.<br/>
    /// </summary>
    /// <param name="minimumLength">The minimum length of the array.</param>
    /// <returns>A fixed-length byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Owner Rent(int minimumLength) => new(this, this.arrayPool.Rent(minimumLength));

    /// <summary>
    /// Gets the maximum length of a byte array instance that may be stored in the pool.
    /// </summary>
    public int MaxLength { get; }

    /// <summary>
    /// Gets the maximum number of array instances that may be stored in each bucket in the pool.
    /// </summary>
    public int MaxPool { get; }

    private ArrayPool<byte> arrayPool;
}
