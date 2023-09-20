// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using LP;
using Tinyhand;

namespace Benchmark;

[TinyhandObject]
public partial class SignatureTestClass
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [Key(2)]
    public double Age { get; set; }

    [Key(3)]
    public int[] IntArray { get; set; } = Array.Empty<int>();
}

[Config(typeof(BenchmarkConfig))]
public class SignatureBenchmark
{
    [Params(10)]
    public int Length { get; set; }

    public SignatureTestClass Class { get; set; }

    public SignatureBenchmark()
    {
        this.Class = new();
        this.Class.Id = 10000;
        this.Class.Name = "HogeHoge";
        this.Class.Age = 10000d;
        this.Class.IntArray = new int[] { 1, 2, 3, 10, 20, 30, 100, 200, 300, 1000, };
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
    public Identifier GetIdentifier()
    {
        var bin = TinyhandSerializer.SerializeObject(this.Class, TinyhandSerializerOptions.Signature);
        return Identifier.FromReadOnlySpan(bin);
    }

    /*[Benchmark]
    public Identifier GetIdentifier2()
    {
        var identifier = Hash.GetIdentifier(this.Class, 0x40000000);
        return identifier;
    }*/
}
