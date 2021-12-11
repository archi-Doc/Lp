// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;
using Benchmark.Design;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class IdentifierBenchmark
{
    public IdentifierBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.ByteArray = new byte[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            this.ByteArray[i] = (byte)i;
        }

        this.Destination = new byte[32];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Params(10)]
    public int Length { get; set; }

    public byte[] ByteArray { get; set; } = default!;

    public byte[] Destination { get; set; } = default!;

    public Sha3_256 Instance { get; } = new();

    [Benchmark]
    public Identifier_ClassULong New_ClassULong()
    {
        var r = this.Instance.GetHashUInt64(this.ByteArray);
        return new Identifier_ClassULong(r.hash0, r.hash1, r.hash2, r.hash3);
    }

    [Benchmark]
    public Identifier_ClassULong New_ClassULong2()
    {
        var r = this.Instance.GetHashUInt64(this.ByteArray);
        return new Identifier_ClassULong(r);
    }

    [Benchmark]
    public Identifier_ClassByte New_ClassByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        return new Identifier_ClassByte(r);
    }

    [Benchmark]
    public Identifier_StructByte New_StructByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        return new Identifier_StructByte(r);
    }

    [Benchmark]
    public byte[] NewAndWrite_ClassULong()
    {
        var r = this.Instance.GetHashUInt64(this.ByteArray);
        var i = new Identifier_ClassULong(r);
        i.TryWriteBytes(this.Destination);
        return this.Destination;
    }

    [Benchmark]
    public byte[] NewAndWrite_ClassByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        var i = new Identifier_ClassByte(r);
        i.TryWriteBytes(this.Destination);
        return this.Destination;
    }

    [Benchmark]
    public byte[] NewAndWrite_StructByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        var i = new Identifier_StructByte(r);
        i.TryWriteBytes(this.Destination);
        return this.Destination;
    }
}
