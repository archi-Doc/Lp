// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Netsphere.Crypto;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class SignaturePublicKeyBenchmark
{
    public const int Length = 3;

    private SignaturePublicKey[] array;
    private SignaturePublicKey target;

    public SignaturePublicKeyBenchmark()
    {
        this.array = new SignaturePublicKey[Length];
        for (var i = 0; i < Length; i++)
        {
            this.array[i] = SeedKey.NewSignature().GetSignaturePublicKey();
        }

        this.target = this.array[1];
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
    public int Test_IndexOf()
        => this.IndexOf(this.target);

    [Benchmark]
    public int Test_Find()
        => this.Find(this.target);

    [Benchmark]
    public int Test_FindRef()
        => this.FindRef(ref this.target);

    [Benchmark]
    public int Test_FindRefRef()
        => this.FindRefRef(ref this.target);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int IndexOf(SignaturePublicKey publicKey)
        => this.array.IndexOf(publicKey);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int Find(SignaturePublicKey publicKey)
    {
        var count = this.array.Length;
        if (count == 0)
        {
            return -1;
        }
        else if (this.array[0].Equals(publicKey))
        {
            return 0;
        }

        if (count == 1)
        {
            return -1;
        }
        else if (this.array[1].Equals(publicKey))
        {
            return 1;
        }

        if (count == 2)
        {
            return -1;
        }
        else if (this.array[2].Equals(publicKey))
        {
            return 2;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int FindRef(ref SignaturePublicKey publicKey)
    {
        var count = this.array.Length;
        if (count == 0)
        {
            return -1;
        }
        else if (this.array[0].Equals(publicKey))
        {
            return 0;
        }

        if (count == 1)
        {
            return -1;
        }
        else if (this.array[1].Equals(publicKey))
        {
            return 1;
        }

        if (count == 2)
        {
            return -1;
        }
        else if (this.array[2].Equals(publicKey))
        {
            return 2;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int FindRefRef(ref SignaturePublicKey publicKey)
    {
        var count = this.array.Length;
        if (count == 0)
        {
            return -1;
        }
        else if (this.array[0].Equals(ref publicKey))
        {
            return 0;
        }

        if (count == 1)
        {
            return -1;
        }
        else if (this.array[1].Equals(ref publicKey))
        {
            return 1;
        }

        if (count == 2)
        {
            return -1;
        }
        else if (this.array[2].Equals(ref publicKey))
        {
            return 2;
        }

        return -1;
    }
}
