// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Tinyhand;

namespace Benchmark;

[TinyhandObject]
public readonly partial struct StructKey32
{
    [Key(0)]
    private readonly ulong x1;

    [Key(1)]
    private readonly ulong x2;

    [Key(2)]
    private readonly ulong x3;

    [Key(3)]
    private readonly ulong x4;

    public StructKey32(ulong x1, ulong x2, ulong x3, ulong x4)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
        this.x4 = x4;
    }

    public ulong Sum()
        => this.x1 + this.x2 + this.x3 + this.x4;
}

[TinyhandObject]
public partial class ClassKey32
{
    [Key(0)]
    private readonly ulong x1;

    [Key(1)]
    private readonly ulong x2;

    [Key(2)]
    private readonly ulong x3;

    [Key(3)]
    private readonly ulong x4;

    public ClassKey32()
    {
    }

    public ClassKey32(ulong x1, ulong x2, ulong x3, ulong x4)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
        this.x4 = x4;
    }

    public ulong Sum()
        => this.x1 + this.x2 + this.x3 + this.x4;
}

[TinyhandObject]
public partial class ProofStruct
{
    [Key(0)]
    public StructKey32 Key { get; set; }

    [Key(1)]
    public ulong Data { get; set; }

    public ProofStruct()
    {
    }

    public ProofStruct(StructKey32 key, ulong data)
    {
        this.Key = key;
        this.Data = data;
    }
}

[TinyhandObject]
public partial class ProofClass
{
    [Key(0)]
    public ClassKey32 Key { get; set; } = default!;

    [Key(1)]
    public ulong Data { get; set; }

    public ProofClass()
    {
    }

    public ProofClass(ClassKey32 key, ulong data)
    {
        this.Key = key;
        this.Data = data;
    }
}

[TinyhandObject]
public partial class LinkageStruct
{
    [Key(0)]
    public ProofStruct Proof { get; set; } = default!;

    [Key(1)]
    public ulong Data { get; set; }

    public LinkageStruct()
    {
    }

    public LinkageStruct(StructKey32 key, ulong data)
    {
        this.Proof = new(key, data);
        this.Data = data;
    }
}

[TinyhandObject]
public partial class LinkageClass
{
    [Key(0)]
    public ProofClass Proof { get; set; } = default!;

    [Key(1)]
    public ulong Data { get; set; }

    public LinkageClass()
    {
    }

    public LinkageClass(ClassKey32 key, ulong data)
    {
        this.Proof = new(key, data);
        this.Data = data;
    }
}

[Config(typeof(BenchmarkConfig))]
public class KeyBenchmark
{
    private const ulong X1 = 0x123456789abcdef0;
    private const ulong X2 = 0x23456789abcdef01;
    private const ulong X3 = 0x3456789abcdef012;
    private const ulong X4 = 0x456789abcdef0123;

    private readonly StructKey32 structKey32 = new(X1, X2, X3, X4);
    private readonly ClassKey32 classKey32 = new(X1, X2, X3, X4);
    private readonly ProofStruct proofStruct = new(new(X1, X2, X3, X4), 0x123456789abcdef0);
    private readonly ProofClass proofClass = new(new(X1, X2, X3, X4), 0x123456789abcdef0);

    private readonly LinkageStruct linkageStruct;
    private readonly LinkageClass linkageClass;
    private readonly byte[] linkageStructBytes;
    private readonly byte[] linkageClassBytes;

    public KeyBenchmark()
    {
        this.linkageStruct = new(this.structKey32, 123456789);
        this.linkageClass = new(this.classKey32, 123456789);
        this.linkageStructBytes = TinyhandSerializer.SerializeObject(this.linkageStruct);
        this.linkageClassBytes = TinyhandSerializer.SerializeObject(this.linkageClass);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    /*[Benchmark]
    public byte[] SerializeStructKey32()
        => TinyhandSerializer.SerializeObject(this.structKey32);

    [Benchmark]
    public byte[] SerializeClassKey32()
        => TinyhandSerializer.SerializeObject(this.classKey32);

    [Benchmark]
    public byte[] SerializeProofStruct()
        => TinyhandSerializer.SerializeObject(this.proofStruct);

    [Benchmark]
    public byte[] SerializeProofClass()
        => TinyhandSerializer.SerializeObject(this.proofClass);*/

    [Benchmark]
    public byte[] SerializeLinkageStruct()
        => TinyhandSerializer.SerializeObject(this.linkageStruct);

    [Benchmark]
    public byte[] SerializeLinkageClass()
        => TinyhandSerializer.SerializeObject(this.linkageClass);

    [Benchmark]
    public LinkageStruct DeserializeLinkageStruct()
        => TinyhandSerializer.DeserializeObject<LinkageStruct>(this.linkageStructBytes)!;

    [Benchmark]
    public LinkageClass DeserializeLinkageClass()
        => TinyhandSerializer.DeserializeObject<LinkageClass>(this.linkageClassBytes)!;

    [Benchmark]
    public ulong DeserializeLinkageStruct2()
    {
        var x = TinyhandSerializer.DeserializeObject<LinkageStruct>(this.linkageStructBytes)!;
        return x.Proof.Key.Sum();
    }

    [Benchmark]
    public ulong DeserializeLinkageClass2()
    {
        var x = TinyhandSerializer.DeserializeObject<LinkageClass>(this.linkageClassBytes)!;
        return x.Proof.Key.Sum();
    }
}
