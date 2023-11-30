// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class AesBenchmark
{
    [Params(10)]
    public int Length { get; set; }

    public ObjectPool<Aes> AesPool { get; } = new(static () => Aes.Create());

    public ObjectPool<Sha3_256> Sha3Pool { get; } = new(static () => new Sha3_256());

    public Aes Aes { get; }

    public byte[] Key { get; }

    public byte[] Iv { get; }

    public byte[] Source { get; }

    public byte[] Destination { get; }

    private readonly object syncAes = new();
    private Aes? aes0;
    private Aes? aes1;

    public AesBenchmark()
    {
        this.Aes = Aes.Create();
        this.Key = new byte[16];
        Array.Fill<byte>(this.Key, 0);
        this.Aes.Key = this.Key;
        this.Iv = new byte[16];
        Array.Fill<byte>(this.Iv, 1);
        this.Source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
        this.Destination = new byte[32];
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
    public byte[] CreateEncrypt()
    {
        using (var aes = Aes.Create())
        {
            return aes.EncryptCbc(this.Source, this.Iv);
        }
    }

    [Benchmark]
    public byte[] Encrypt()
    {
        return this.Aes.EncryptCbc(this.Source, this.Iv);
    }

    [Benchmark]
    public byte[] PoolEncrypt()
    {
        var aes = this.AesPool.Get();
        var result = aes.EncryptCbc(this.Source, this.Iv);
        this.AesPool.Return(aes);
        return result;
    }

    [Benchmark]
    public byte[] PoolEncrypt2()
    {
        var aes = this.AesPool.Get();
        aes.Key = this.Key;
        var result = aes.EncryptCbc(this.Source, this.Iv);
        this.AesPool.Return(aes);
        return result;
    }

    [Benchmark]
    public byte[] PoolEncrypt3()
    {
        var aes = this.RentAes();
        this.Source.AsSpan().CopyTo(this.Destination);
        aes.TryEncryptCbc(this.Destination.AsSpan(0, this.Source.Length), this.Iv, this.Destination, out var written);
        this.ReturnAes(aes);
        return this.Destination;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Aes RentAes()
    {
        lock (this.syncAes)
        {
            Aes aes;
            if (this.aes0 is not null)
            {
                aes = this.aes0;
                this.aes0 = this.aes1;
                this.aes1 = default;
                return aes;
            }
            else
            {
                aes = Aes.Create();
                aes.Key = this.Key;
                return aes;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnAes(Aes aes)
    {
        lock (this.syncAes)
        {
            if (this.aes0 is null)
            {
                this.aes0 = aes;
                return;
            }
            else if (this.aes1 is null)
            {
                this.aes1 = aes;
                return;
            }
            else
            {
                aes.Dispose();
            }
        }
    }

    /*[Benchmark]
    public byte[] PoolHashEncrypt()
    {
        var aes = this.AesPool.Get();
        var sha = this.Sha3Pool.Get();

        aes.Key = this.Key;
        var iv = sha.GetHash(this.Key); // Slow...
        var result = aes.TryEncryptCbc(this.Source, iv.AsSpan(0, 16), this.Destination, out var written);

        this.Sha3Pool.Return(sha);
        this.AesPool.Return(aes);
        return this.Destination;
    }*/
}
