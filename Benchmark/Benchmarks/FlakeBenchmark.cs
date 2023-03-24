// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using LP;
using LP.Crystal;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS0169
#pragma warning disable CS0649

namespace Benchmark.Flake;

[TinyhandObject]
public partial struct DataObject : ITinyhandSerialize<DataObject>
{
    public DataObject()
    {
    }

    [Key(0)]
    internal int Data;

    [Key(1)]
    internal ulong File;

    internal object? Object;

    public static void Serialize(ref TinyhandWriter writer, scoped ref DataObject value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Data);
        writer.Write(value.File);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref DataObject value, TinyhandSerializerOptions options)
    {
        value.Data = reader.ReadInt32();
        value.File = reader.ReadUInt64();
    }
}

[ValueLinkObject]
[TinyhandObject(ExplicitKeyOnly = true, LockObject = nameof(syncObject))]
public partial class Flake2<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    public Flake2()
    {
        this.DataObject = new DataObject[2];
        this.DataObject[0].Data = 1;
        this.DataObject[0].File = 0x1234567812345678;
        this.DataObject[1].Data = 1;
        this.DataObject[1].File = 0x1234567812345678;
    }

    [Key(0)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal TIdentifier identifier = default!;

    [Key(1)]
    internal Flake<TIdentifier>.GoshujinClass? childFlakes;

    [Key(2)]
    public int FlakeId { get; private set; }

    [Key(3)]
    internal DataObject[] DataObject;

    private object syncObject = new();
}

[ValueLinkObject]
[TinyhandObject(ExplicitKeyOnly = true, LockObject = nameof(syncObject))]
public partial class Flake<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    public Flake()
    {
        this.data1 = 1;
        this.file1 = 0x1234567812345678;
        this.data2 = 2;
        this.file2 = 0x1234567812345678;
    }

    [Key(0)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    internal TIdentifier identifier = default!;

    [Key(1)]
    internal Flake<TIdentifier>.GoshujinClass? childFlakes;

    [Key(2)]
    public int FlakeId { get; private set; }

    [Key(3)]
    internal int data1;

    [Key(4)]
    internal ulong file1;

    [Key(5)]
    internal int data2;

    [Key(6)]
    internal ulong file2;

    [Key(7)]
    internal int data3;

    [Key(8)]
    internal ulong file3;

    [Key(9)]
    internal int data4;

    [Key(10)]
    internal ulong file4;

    private object syncObject = new();
    private object? object1;
    private object? object2;
    private object? object3;
    private object? object4;
}

[Config(typeof(BenchmarkConfig))]
public class FlakeBenchmark
{
    public const int N = 1000;

    private LP.Crystal.LpData rootFlake;
    private byte[] rootFlakeBinary;

    private Flake<Identifier> flake;
    private byte[] flakeBinary;
    private Flake<Identifier>.GoshujinClass goshujin;
    private byte[] goshujinBinary;

    private Flake2<Identifier> flake2;
    private byte[] flake2Binary;
    private Flake2<Identifier>.GoshujinClass goshujin2;
    private byte[] goshujin2Binary;

    public FlakeBenchmark()
    {
        this.flake = new();
        this.flakeBinary = this.Serialize();

        this.goshujin = new();
        for (var i = 0; i < N; i++)
        {
            new Flake<Identifier>().Goshujin = this.goshujin;
        }

        this.goshujinBinary = TinyhandSerializer.SerializeObject(this.goshujin);

        this.flake2 = new();
        this.flake2Binary = this.Serialize2();

        this.goshujin2 = new();
        for (var i = 0; i < N; i++)
        {
            new Flake2<Identifier>().Goshujin = this.goshujin2;
        }

        this.goshujin2Binary = TinyhandSerializer.SerializeObject(this.goshujin2);

        this.rootFlake = (LpData)Activator.CreateInstance(typeof(LpData), true)!;
        for (var i = 0; i < 1_000_000; i++)
        {
            this.rootFlake.GetOrCreateChild(new(i));
        }

        this.rootFlakeBinary = TinyhandSerializer.SerializeObject(this.rootFlake);
        var f = TinyhandSerializer.DeserializeObject<LP.Crystal.LpData>(this.rootFlakeBinary);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // this.crystal.StopAsync(new(RemoveAll: true)).Wait();
    }

    // [Benchmark]
    public byte[] Serialize()
    {
        return TinyhandSerializer.SerializeObject(this.flake);
    }

    // [Benchmark]
    public byte[] Serialize2()
    {
        return TinyhandSerializer.SerializeObject(this.flake2);
    }

    [Benchmark]
    public byte[] SerializeZen()
    {
        return TinyhandSerializer.SerializeObject(this.rootFlake);
    }

    // [Benchmark]
    public Flake<Identifier>? Deserialize()
    {
        return TinyhandSerializer.DeserializeObject<Flake<Identifier>>(this.flakeBinary);
    }

    // [Benchmark]
    public Flake2<Identifier>? Deserialize2()
    {
        return TinyhandSerializer.DeserializeObject<Flake2<Identifier>>(this.flake2Binary);
    }

    [Benchmark]
    public object? DeserializeZen()
    {// Zen<Identifier>.Flake
        return TinyhandSerializer.DeserializeObject<LpData>(this.rootFlakeBinary);
    }

    /*[Benchmark]
    public byte[] SerializeGoshujin()
    {
        return TinyhandSerializer.SerializeObject(this.goshujin);
    }

    [Benchmark]
    public byte[] SerializeGoshujin2()
    {
        return TinyhandSerializer.SerializeObject(this.goshujin2);
    }

    [Benchmark]
    public object? DeserializeGoshujin()
    {// Flake<Identifier>.GoshujinClass
        return TinyhandSerializer.DeserializeObject<Flake<Identifier>.GoshujinClass>(this.goshujinBinary);
    }

    [Benchmark]
    public object? DeserializeGoshujin2()
    {// Flake2<Identifier>.GoshujinClass
        return TinyhandSerializer.DeserializeObject<Flake2<Identifier>.GoshujinClass>(this.goshujin2Binary);
    }*/
}
