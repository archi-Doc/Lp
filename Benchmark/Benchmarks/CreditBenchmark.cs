// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using LP.T3CS;
using Tinyhand;

namespace Benchmark;

[TinyhandObject]
public sealed partial class Credit
{
    public const int MaxMergers = 4;

    public Credit()
    {
    }

    public Credit(PublicKey key, int mergers)
    {
        this.Originator = key;
        this.mergers = new PublicKey[mergers];
        for (var i = 0; i < mergers; i++)
        {
            this.mergers[i] = key;
        }

        this.Standard = key;
    }

    [Key(0)]
    public PublicKey Originator { get; private set; } = default!;

    [Key(1, AddProperty = "Mergers")]
    [MaxLength(MaxMergers)]
    private LP.T3CS.PublicKey[] mergers = Array.Empty<PublicKey>();

    [Key(2)]
    public PublicKey Standard { get; private set; } = default!;
}

[TinyhandObject]
public sealed partial class CreditB
{
    public const int MaxMergers = 3;

    public CreditB()
    {
    }

    public CreditB(PublicKey key, int mergers)
    {
        this.Originator = key;
        this.Standard = key;
        this.Merger1 = key;

        if (--mergers > 0)
        {
            // this.Merger2 = key;
        }

        if (--mergers > 0)
        {
            // this.Merger3 = key;
        }
    }

    [Key(0)]
    public PublicKey Originator { get; private set; } = default!;

    [Key(1)]
    public PublicKey Standard { get; private set; } = default!;

    [Key(2)]
    public PublicKey Merger1 { get; private set; } = default!;

    /*[Key(3)]
    public LP.T3CS.PublicKey? Merger2 { get; private set; } = default!;

    [Key(4)]
    public LP.T3CS.PublicKey? Merger3 { get; private set; } = default!;*/
}

[Config(typeof(BenchmarkConfig))]
public class CreditBenchmark
{
    public PrivateKey PrivateKey { get; private set; }

    public PublicKey PublicKey { get; private set; }

    public Benchmark.Credit Credit1 { get; private set; }

    public Benchmark.Credit Credit3 { get; private set; }

    public Benchmark.CreditB CreditB1 { get; private set; }

    public Benchmark.CreditB CreditB3 { get; private set; }

    public byte[] Byte1 { get; private set; }

    public byte[] Byte3 { get; private set; }

    public byte[] ByteB1 { get; private set; }

    public byte[] ByteB3 { get; private set; }

    public CreditBenchmark()
    {
        this.PrivateKey = PrivateKey.CreateVerificationKey(new byte[] { 0, 1, 2, 3, });
        this.PublicKey = this.PrivateKey.ToPublicKey();

        this.Credit1 = new(this.PublicKey, 1);
        this.Credit3 = new(this.PublicKey, 3);
        this.CreditB1 = new(this.PublicKey, 1);
        this.CreditB3 = new(this.PublicKey, 3);

        this.Byte1 = TinyhandSerializer.SerializeObject(this.Credit1);
        this.Byte3 = TinyhandSerializer.SerializeObject(this.Credit3);
        this.ByteB1 = TinyhandSerializer.SerializeObject(this.CreditB1);
        this.ByteB3 = TinyhandSerializer.SerializeObject(this.CreditB3);
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
    public byte[] Serialize_Credit1()
    {
        return TinyhandSerializer.SerializeObject(this.Credit1);
    }

    [Benchmark]
    public byte[] Serialize_CreditB1()
    {
        return TinyhandSerializer.SerializeObject(this.CreditB1);
    }

    [Benchmark]
    public byte[] Serialize_Credit3()
    {
        return TinyhandSerializer.SerializeObject(this.Credit3);
    }

    [Benchmark]
    public byte[] Serialize_CreditB3()
    {
        return TinyhandSerializer.SerializeObject(this.CreditB3);
    }

    [Benchmark]
    public Benchmark.Credit? Deserialize_Credit1()
    {
        return TinyhandSerializer.DeserializeObject<Benchmark.Credit>(this.Byte1);
    }

    [Benchmark]
    public Benchmark.CreditB? Deserialize_CreditB1()
    {
        return TinyhandSerializer.DeserializeObject<Benchmark.CreditB>(this.ByteB1);
    }

    [Benchmark]
    public Benchmark.Credit? Deserialize_Credit3()
    {
        return TinyhandSerializer.DeserializeObject<Benchmark.Credit>(this.Byte3);
    }

    [Benchmark]
    public Benchmark.CreditB? Deserialize_CreditB3()
    {
        return TinyhandSerializer.DeserializeObject<Benchmark.CreditB>(this.ByteB3);
    }
}
