// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Crypto;

namespace LP.Net;

internal class GenePool : IDisposable
{
    public const int PoolLength = 64;
    public const int PoolSize = PoolLength * sizeof(ulong);
    public const int EmbryoMax = 1024;

    public GenePool()
    {
        this.OriginalGene = Random.Crypto.NextULong();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetGene()
    {
        if (this.aes != null)
        {// Managed
            this.EnsurePool();
            return this.pool![this.poolPosition++];
        }
        else
        {// Unmanaged
            if (this.pseudoRandom == null)
            {
                this.pseudoRandom = new(this.OriginalGene);
                return this.OriginalGene;
            }
            else
            {
                return this.pseudoRandom.NextULong();
            }
        }
    }

    public void SetEmbryo(byte[] embryo)
    {
        if (this.pseudoRandom == null)
        {
            this.pseudoRandom = new(this.OriginalGene);
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

        if (this.aes == null)
        {
            this.aes = Aes.Create();
        }

        this.aes.Padding = PaddingMode.None;
        this.aes.Mode = CipherMode.CBC;
        this.aes.Key = keyIv.AsSpan(0, 32).ToArray();
        this.aes.IV = keyIv.AsSpan(32, 16).ToArray();

        if (this.encryptor != null)
        {
            this.encryptor.Dispose();
        }

        var encryptor = this.aes.CreateEncryptor(keyIv.AsSpan(0, 32).ToArray(), keyIv.AsSpan(32, 16).ToArray());
        this.encryptor = this.aes.CreateEncryptor(keyIv.AsSpan(0, 32).ToArray(), keyIv.AsSpan(32, 16).ToArray());
        this.EnsurePool2(encryptor);
    }

    public ulong OriginalGene { get; }

    private void EnsurePool2(ICryptoTransform encryptor)
    {
        if (this.pool != null && this.poolPosition < PoolLength)
        {
            return;
        }

        this.poolPosition = 0;
        if (this.pool == null)
        {
            this.pool = new ulong[PoolLength];
        }

        if (this.bytePool == null)
        {
            this.bytePool = new byte[PoolSize];
        }

        var buffer2 = new byte[PoolSize];
        this.pseudoRandom!.NextBytes(this.bytePool);

        var blockSize = encryptor.InputBlockSize;
        encryptor.TransformBlock(this.bytePool, 0, PoolSize, buffer2, 0);

        this.aes!.TryEncryptCbc(this.bytePool, this.aes!.IV, MemoryMarshal.AsBytes<ulong>(this.pool), out var written, PaddingMode.None);
        this.aes!.TryEncryptCbc(this.bytePool, this.aes!.IV, MemoryMarshal.AsBytes<ulong>(this.pool), out written, PaddingMode.None);
    }

    private void EnsurePool()
    {
        if (this.pool != null && this.poolPosition < PoolLength)
        {
            return;
        }

        this.poolPosition = 0;
        if (this.pool == null)
        {
            this.pool = new ulong[PoolLength];
        }

        Span<byte> buffer = stackalloc byte[PoolSize];
        this.pseudoRandom!.NextBytes(buffer);

        this.aes!.TryEncryptCbc(buffer, this.aes!.IV, MemoryMarshal.AsBytes<ulong>(this.pool), out var written, PaddingMode.None);
    }

    private Xoshiro256StarStar? pseudoRandom;
    private Aes? aes;
    private ICryptoTransform? encryptor;
    private ulong[]? pool;
    private byte[]? bytePool;
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
                this.aes?.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
