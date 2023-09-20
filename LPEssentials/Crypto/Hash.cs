// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace LP;

public class Hash : Sha3_256
{
    public static readonly new string HashName = "SHA3-256";
    public static readonly new uint HashBits = 256;
    public static readonly new uint HashBytes = HashBits / 8;
    public static readonly int BufferLength = 1024;

    public static ObjectPool<Hash> ObjectPool { get; } = new(static () => new Hash());

    private byte[] buffer = new byte[BufferLength];

    public Identifier GetIdentifier<T>(T? value, int level)
        where T : ITinyhandSerialize<T>
    {
        var writer = new TinyhandWriter(this.buffer) { Level = level, };
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            return new Identifier(Sha3Helper.Get256_UInt64(writer.FlushAndGetReadOnlySpan()));
        }
        finally
        {
            writer.Dispose();
        }
    }

    public ulong GetFarmHash<T>(T? value)
        where T : ITinyhandSerialize<T>
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Selection);
            return FarmHash.Hash64(writer.FlushAndGetReadOnlySpan());
        }
        finally
        {
            writer.Dispose();
        }
    }
}
