// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class IdentifierBenchmark
{
    public IdentifierBenchmark()
    {
        this.ByteArray = new byte[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            this.ByteArray[i] = (byte)i;
        }
    }

    [Params(10)]
    public int Length { get; set; }

    public byte[] ByteArray { get; } = default!;

    public SHA3_256 Instance { get; } = new();

    [Benchmark]
    public Identifier_ClassULong Identifier_ClassULong()
    {
        var r = this.Instance.GetHashULong(this.ByteArray);
        return new Identifier_ClassULong(r.hash0, r.hash1, r.hash2, r.hash3);
    }

    [Benchmark]
    public Identifier_ClassByte Identifier_ClassByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        return new Identifier_ClassByte(r);
    }

    [Benchmark]
    public Identifier_StructByte Identifier_StructByte()
    {
        var r = this.Instance.GetHash(this.ByteArray);
        return new Identifier_StructByte(r);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }
}
