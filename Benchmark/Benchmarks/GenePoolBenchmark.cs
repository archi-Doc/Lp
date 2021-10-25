// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Security.Cryptography;
using Arc.Crypto;
using Benchmark.Design;
using BenchmarkDotNet.Attributes;
using LP;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class GenePoolBenchmark
{
    [Params(100)]
    public int Length { get; set; }

    public byte[] Source { get; }

    public Xoshiro256StarStar Xoshiro { get; } = new();

    public MersenneTwister Mt { get; } = new();

    public GenePoolBenchmark()
    {
        this.Source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, };
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public Aes NewAes()
    {
        return Aes.Create();
    }

    [Benchmark]
    public ulong[] Xoshiro_NextULong()
    {
        var array = new ulong[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            array[i] = this.Xoshiro.NextULong();
        }

        return array;
    }

    [Benchmark]
    public ulong[] Mt_NextULong()
    {
        var array = new ulong[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            array[i] = this.Mt.NextULong();
        }

        return array;
    }

    [Benchmark]
    public byte[] GenerateGenePool()
    {
        return this.GenerateGenePool(this.Source, this.Length * sizeof(ulong));
    }

    [Benchmark]
    public byte[] GenerateGenePool2()
    {
        return this.GenerateGenePool2(this.Source, this.Length * sizeof(ulong));
    }

    public unsafe byte[] GenerateGenePool(ReadOnlySpan<byte> source, int destinationSize)
    { // length <= 3k ?, 3k x 8 bytes = 24 kbytes
        var hash = XXHash64.Hash64(source);
        var xo = new Xoshiro256StarStar(hash);

        Span<byte> span = stackalloc byte[destinationSize];
        xo.NextBytes(span);

        var aes = Aes.Create();
        aes.KeySize = 128;

        // Span<byte> key = stackalloc byte[16];
        var key = new byte[16];
        xo.NextBytes(key);
        aes.Key = key;
        // Span<byte> iv = stackalloc byte[16];
        var iv = new byte[16];
        xo.NextBytes(iv);
        aes.IV = iv;

        // MemoryMarshal.AsBytes(span);
        var result = aes.EncryptCbc(span, iv);
        return result;

        // var encryptor = aes.CreateEncryptor(key, iv);
    }

    public unsafe byte[] GenerateGenePool2(ReadOnlySpan<byte> source, int destinationSize)
    { // length <= 3k ?, 3k x 8 bytes = 24 kbytes
        var hash = XXHash64.Hash64(source);
        var xo = new Xoshiro256StarStar(hash);

        byte[]? rentBytes = null;
        Span<byte> span = destinationSize <= 1024 ? stackalloc byte[destinationSize] : (rentBytes = ArrayPool<byte>.Shared.Rent(destinationSize));
        xo.NextBytes(span);

        var aes = Aes128.ObjectPool.Get();
        try
        {
            /*xo.NextBytes(aes.Key);
            xo.NextBytes(aes.IV);
            var encryptor = aes.Aes.CreateEncryptor(aes.Key, aes.IV);

            var result = new byte[destinationSize];
            var input = result.AsSpan();
            var blockSize = aes.Aes.BlockSize;
            var blocks = destinationSize / blockSize;
            var inputOffset = 0;
            for (var i = 0; i < blocks; i++)
            {
                encryptor.tr
                encryptor.TransformBlock(span, inputOffset, destinationSize, result, 0);
                inputOffset += blockSize;
                input = input.Slice(blockSize);
            }*/

            xo.NextBytes(aes.Key);
            aes.Aes.Key = aes.Key;
            xo.NextBytes(aes.IV);
            // aes.Aes.IV = aes.IV;
            var result = new byte[destinationSize];
            aes.Aes.TryEncryptCbc(span, aes.IV, result, out var written, PaddingMode.None);

            /*xo.NextBytes(aes.Key);
            aes.Aes.Key = aes.Key;
            xo.NextBytes(aes.IV);
            aes.Aes.IV = aes.IV;
            var result = aes.Aes.EncryptCbc(span, aes.IV, PaddingMode.None);*/
            return result;
        }
        finally
        {
            Aes128.ObjectPool.Return(aes);
            if (rentBytes != null)
            {
                ArrayPool<byte>.Shared.Return(rentBytes);
            }
        }

        // MemoryMarshal.AsBytes(span);
    }
}
