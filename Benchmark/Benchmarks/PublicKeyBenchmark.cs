// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;
using LP;
using LP.T3CS;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark;

[TinyhandObject]
public readonly partial struct PublicKeyStruct
{
    public PublicKeyStruct()
    {
    }

    internal PublicKeyStruct(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = keyValue;
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
    }

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public ulong GetChecksum()
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = this.keyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
        b = b.Slice(sizeof(ulong));

        written = KeyHelper.EncodedLength;
        return true;
    }
}

[TinyhandObject]
public sealed partial class PublicKeyClass
{
    public PublicKeyClass()
    {
    }

    internal PublicKeyClass(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = keyValue;
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
    }

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public ulong GetChecksum()
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = this.keyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
        b = b.Slice(sizeof(ulong));

        written = KeyHelper.EncodedLength;
        return true;
    }
}

public interface IPublicKeyInterface
{
    byte KeyValue { get; }

    ulong X0 { get; }

    ulong X1 { get; }

    ulong X2 { get; }

    ulong X3 { get; }
}

[TinyhandObject]
public readonly partial struct PublicKeyInterface : IPublicKeyInterface
{
    public PublicKeyInterface()
    {
    }

    internal PublicKeyInterface(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = keyValue;
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
    }

    public byte KeyValue => this.keyValue;

    ulong IPublicKeyInterface.X0 => this.x0;

    ulong IPublicKeyInterface.X1 => this.x1;

    ulong IPublicKeyInterface.X2 => this.x2;

    ulong IPublicKeyInterface.X3 => this.x3;

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;
}

public static class PublicKeyBenchmarkHelper
{
    public static ulong GetChecksum(this IPublicKeyInterface obj)
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        obj.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public static bool TryWriteBytes(this IPublicKeyInterface obj, Span<byte> span, out int written)
    {
        if (span.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = obj.KeyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, obj.X0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.X1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.X2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.X3);
        b = b.Slice(sizeof(ulong));

        written = KeyHelper.EncodedLength;
        return true;
    }

    public static ulong GetChecksum<T>(this T obj)
        where T : IPublicKeyInterface2<T>
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        T.TryWriteBytes(obj, span, out _);
        return FarmHash.Hash64(span);
    }
}

public interface IPublicKeyInterface2<T>
{
    static abstract bool TryWriteBytes(T obj, Span<byte> span, out int written);
}

[TinyhandObject]
public readonly partial struct PublicKeyStruct2 : IPublicKeyInterface2<PublicKeyStruct2>
{
    public PublicKeyStruct2()
    {
    }

    internal PublicKeyStruct2(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = keyValue;
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
    }

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public static bool TryWriteBytes(PublicKeyStruct2 obj, Span<byte> span, out int written)
    {
        if (span.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = obj.keyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, obj.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, obj.x3);
        b = b.Slice(sizeof(ulong));

        written = KeyHelper.EncodedLength;
        return true;
    }
}

[Config(typeof(BenchmarkConfig))]
public class PublicKeyBenchmark
{
    private byte[] buffer = new byte[1024];
    private PublicKeyStruct publicKeyStruct;
    private PublicKeyClass publicKeyClass;
    private PublicKeyInterface publicKeyInterface;
    private PublicKeyStruct2 publicKeyStruct2;

    public PublicKeyBenchmark()
    {
        this.publicKeyStruct = new(123, 0x1234_1234_1234_1234, 1234, 0x_ABCD_ABCD_ABCD_ABCD, 4321);
        this.publicKeyClass = new(123, 0x1234_1234_1234_1234, 1234, 0x_ABCD_ABCD_ABCD_ABCD, 4321);
        this.publicKeyInterface = new(123, 0x1234_1234_1234_1234, 1234, 0x_ABCD_ABCD_ABCD_ABCD, 4321);
        this.publicKeyStruct2 = new(123, 0x1234_1234_1234_1234, 1234, 0x_ABCD_ABCD_ABCD_ABCD, 4321);
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
    public ulong GetCheckSum_Struct()
        => this.publicKeyStruct.GetChecksum();

    [Benchmark]
    public ulong GetCheckSum_Class()
        => this.publicKeyClass.GetChecksum();

    [Benchmark]
    public ulong GetCheckSum_Interface()
        => this.publicKeyInterface.GetChecksum();

    [Benchmark]
    public ulong GetCheckSum_Struct2()
        => this.publicKeyStruct2.GetChecksum();

    [Benchmark]
    public PublicKeyStruct SerializeDeserialize_Struct()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.publicKeyStruct);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            var result = TinyhandSerializer.DeserializeObject<PublicKeyStruct>(span);
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public PublicKeyClass SerializeDeserialize_Class()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.publicKeyClass);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            var result = TinyhandSerializer.DeserializeObject<PublicKeyClass>(span)!;
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public PublicKeyInterface SerializeDeserialize_Interface()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.publicKeyInterface);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            var result = TinyhandSerializer.DeserializeObject<PublicKeyInterface>(span);
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Benchmark]
    public PublicKeyStruct2 SerializeDeserialize_Struct2()
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.publicKeyStruct2);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            var result = TinyhandSerializer.DeserializeObject<PublicKeyStruct2>(span);
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
