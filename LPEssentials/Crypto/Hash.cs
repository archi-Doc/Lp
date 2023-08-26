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

    public static ObjectPool<Sha3_384> Sha3_384Pool { get; } = new(static () => new Sha3_384());

    private byte[] buffer = new byte[BufferLength];

    public Identifier GetIdentifier(ReadOnlySpan<byte> input)
    {
        return new Identifier(this.GetHashUInt64(input));
    }

    public Identifier IdentifierFinal()
    {
        return new Identifier(this.HashFinalUInt64());
    }

    public Identifier GetIdentifier<T>(T? value, TinyhandSerializerOptions? options)
        where T : ITinyhandSerialize<T>
    {
        var writer = new TinyhandWriter(this.buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, options);
            return new Identifier(this.GetHashUInt64(writer.FlushAndGetReadOnlySpan()));
        }
        finally
        {
            writer.Dispose();
        }
    }
}
