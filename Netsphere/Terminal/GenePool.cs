// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Crypto;

namespace Netsphere;

/// <summary>
/// Thread-safe pool of genes.
/// </summary>
internal class GenePool : IDisposable
{
    public const int PoolSize = 16 * sizeof(ulong);
    public const int EmbryoMax = 1024;

    public static ulong NextGene(ulong gene)
        => Arc.Crypto.Xorshift.Xor64(gene);

    public GenePool(ulong first)
    {
        this.currentGene = first;
    }

    private GenePool(GenePool original, byte[] embryo)
    {
        this.pool = new byte[PoolSize];
        var ulongSpan = MemoryMarshal.Cast<byte, ulong>(this.pool);
        original.GetSequential(ulongSpan);

        this.currentGene = ulongSpan[0];
        this.embryo = embryo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ulong GetSequential()
    {
        lock (this.syncObject)
        {
            return this.GetGene();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void GetSequential(Span<ulong> span)
    {
        lock (this.syncObject)
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = this.GetGene();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe (ulong First, ulong Second) GetSequential2()
    {
        lock (this.syncObject)
        {
            return (this.GetGene(), this.GetGene());
        }
    }

    public void SetEmbryo(byte[] embryo)
    {
        lock (this.syncObject)
        {
            if (this.pseudoRandom == null)
            {
                this.pseudoRandom = new(this.currentGene);
            }

            var source = embryo.AsSpan();
            if (source.Length > EmbryoMax)
            {
                source = source.Slice(0, EmbryoMax);
            }

            Span<byte> span = stackalloc byte[sizeof(ulong) + source.Length];
            var buffer = span;
            BitConverter.TryWriteBytes(buffer, this.pseudoRandom.NextUInt64());
            buffer = buffer.Slice(sizeof(ulong));
            source.CopyTo(buffer);

            Span<byte> keyIv = stackalloc byte[48];
            Sha3Helper.Get384_Span(span, keyIv);

            if (this.encryptor != null)
            {
                this.encryptor.Dispose();
            }

            this.encryptor = Aes256.NoPadding.CreateEncryptor(keyIv.Slice(0, 32).ToArray(), keyIv.Slice(32, 16).ToArray());
            this.EnsurePool();
        }
    }

    public GenePool Fork(byte[] embryo)
    {
        if (embryo.Length < 48)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (this.encryptor == null)
        {
            this.SetEmbryo(embryo);
        }

        return new GenePool(this, embryo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ulong GetGene()
    {// lock (this.syncObject)
        if (this.pool != null || this.encryptor != null)
        { // Pooled genes (Managed)
            if (this.poolPosition >= PoolSize)
            {
                this.EnsurePool();
            }

            fixed (byte* bp = this.pool)
            {
                ulong* up = (ulong*)(bp + this.poolPosition);
                this.poolPosition += sizeof(ulong);
                return *up;
            }
        }
        else
        {// Unmanaged
            this.currentGene = NextGene(this.currentGene);
            return this.currentGene;
        }
    }

    private void EnsurePool()
    {// lock (this.syncObject)
        this.poolPosition = 0;
        if (this.pool == null)
        {
            this.pool = new byte[PoolSize];
        }

        if (this.buffer == null)
        {
            this.buffer = new byte[PoolSize];
        }

        if (this.pseudoRandom == null)
        {
            this.pseudoRandom = new(this.currentGene);
        }

        if (this.encryptor == null)
        {
            var iv = new byte[16];
            BitConverter.TryWriteBytes(iv, this.currentGene);
            this.embryo.AsSpan(40, 8).CopyTo(iv.AsSpan(8, 8));
            this.encryptor = Aes256.NoPadding.CreateEncryptor(this.embryo.AsSpan(8, 32).ToArray(), iv);
            this.embryo = null;
        }

        this.pseudoRandom.NextBytes(this.buffer);
        this.encryptor!.TransformBlock(this.buffer, 0, PoolSize, this.pool, 0);

        // this.aes!.TryEncryptCbc(this.bytePool, this.aes!.IV, MemoryMarshal.AsBytes<ulong>(this.pool), out written, PaddingMode.None);
    }

    private object syncObject = new();
    private ulong currentGene;
    private Xoshiro256StarStar? pseudoRandom;
    private ICryptoTransform? encryptor;
    private byte[]? embryo;
    private byte[]? pool;
    private byte[]? buffer;
    private int poolPosition;

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="GenePool"/> class.
    /// </summary>
    ~GenePool()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.encryptor?.Dispose();
                this.encryptor = null;
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
