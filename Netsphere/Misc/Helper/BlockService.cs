// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere.Block;

public static class BlockService
{
    public const int StandardBlockSize = 32 * 1024; // 32KB

    public static TinyhandSerializerOptions SerializerOptions { get; } = TinyhandSerializerOptions.Standard;

    public static bool TrySerialize<T>(T value, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = ByteArrayPool.Default.Rent(StandardBlockSize);
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            TinyhandSerializer.Serialize(ref writer, value, SerializerOptions);

            writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
            if (isInitialBuffer)
            {
                owner = arrayOwner.ToMemoryOwner(0, arrayLength);
                return true;
            }
            else
            {
                arrayOwner.Return();
                owner = new ByteArrayPool.MemoryOwner(array);
                return true;
            }
        }
        catch
        {
            arrayOwner.Return();
            owner = default;
            return false;
        }
    }

    public static bool TryDeserialize<T>(ByteArrayPool.MemoryOwner owner, [MaybeNullWhen(false)] out T value)
        => TinyhandSerializer.TryDeserialize<T>(owner.Memory.Span, out value, SerializerOptions);

    public static bool TryDeserialize<T>(ByteArrayPool.ReadOnlyMemoryOwner owner, [MaybeNullWhen(false)] out T value)
        => TinyhandSerializer.TryDeserialize<T>(owner.Memory.Span, out value, SerializerOptions);
}
