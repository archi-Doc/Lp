// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Crypto;

namespace Netsphere;

internal class GenePool : IDisposable
{
    public const int PoolSize = 64 * sizeof(ulong);
    public const int EmbryoMax = 1024;

    public static void NextGene(ref ulong gene)
        => Arc.Crypto.Xorshift.Xor64(ref gene);

    public static ulong NextGene(ulong gene)
        => Arc.Crypto.Xorshift.Xor64(gene);

    public GenePool(ulong gene)
    {
        this.currentGene = gene;
    }

    public void ResetGene()
    {
        this.currentGene = LP.Random.Crypto.NextULong();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ulong GetGene()
    {
        if (this.encryptor != null)
        {// Managed
            this.EnsurePool();
            fixed (byte* bp = this.pool)
            {
                ulong* up = (ulong*)(bp + this.poolPosition);
                this.poolPosition += sizeof(ulong);
                return *up;
            }

            /*var gene = BitConverter.ToUInt64(this.bytePool.AsSpan(this.poolPosition));
            this.poolPosition += sizeof(ulong);
            return gene;*/
        }
        else
        {// Unmanaged
            NextGene(ref this.currentGene);
            return this.currentGene;
        }
    }

    public void SetEmbryo(byte[] embryo)
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
        BitConverter.TryWriteBytes(buffer, this.pseudoRandom.NextULong());
        buffer = buffer.Slice(sizeof(ulong));
        source.CopyTo(buffer);

        var sha = Hash.Sha3_384Pool.Get();
        var keyIv = sha.GetHash(span);
        Hash.Sha3_384Pool.Return(sha);

        if (this.encryptor != null)
        {
            this.encryptor.Dispose();
        }

        this.encryptor = Aes256.NoPadding.CreateEncryptor(keyIv.AsSpan(0, 32).ToArray(), keyIv.AsSpan(32, 16).ToArray());
    }

    public ulong OriginalGene { get; }

    private void EnsurePool()
    {
        if (this.pool != null && this.poolPosition < PoolSize)
        {
            return;
        }

        this.poolPosition = 0;
        if (this.pool == null)
        {
            this.pool = new byte[PoolSize];
        }

        if (this.buffer == null)
        {
            this.buffer = new byte[PoolSize];
        }

        this.pseudoRandom!.NextBytes(this.buffer);
        this.encryptor!.TransformBlock(this.buffer, 0, PoolSize, this.pool, 0);

        // this.aes!.TryEncryptCbc(this.bytePool, this.aes!.IV, MemoryMarshal.AsBytes<ulong>(this.pool), out written, PaddingMode.None);
    }

    private ulong currentGene;
    private Xoshiro256StarStar? pseudoRandom;
    private ICryptoTransform? encryptor;
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
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
