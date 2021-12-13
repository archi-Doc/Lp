// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LP.Block;

public static class BlockService
{
    public const int MaxBlockSize = 1024 * 1024 * 4; // 4MB
    public const int StandardBlockSize = 32 * 1024; // 32KB
    public const int StandardBlockPool = 400;

    public static TinyhandSerializerOptions SerializerOptions { get; } = TinyhandSerializerOptions.Standard;

    public static uint GetId<T>() => IdCache<T>.Id;

    public static ulong GetId<TSend, TReceive>() => (ulong)IdCache<TSend>.Id | ((ulong)IdCache<TReceive>.Id << 32);

    public static bool TrySerialize<T>(T value, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = BlockPool.Rent(StandardBlockSize);
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            TinyhandSerializer.Serialize(ref writer, value, SerializerOptions);

            writer.FlushAndGetArray(out var array, out var arrayLength);
            if (array != arrayOwner.ByteArray)
            {
                arrayOwner.Return();
                if (arrayLength > MaxBlockSize)
                {
                    owner = default;
                    return false;
                }
                else
                {
                    owner = new ByteArrayPool.MemoryOwner(array);
                    return true;
                }
            }

            owner = arrayOwner.ToMemoryOwner(0, arrayLength);
            return true;
        }
        catch
        {
            arrayOwner.Return();
            owner = default;
            return false;
        }
    }

    public static bool TryDeserialize<T>(ByteArrayPool.MemoryOwner owner, [MaybeNullWhen(false)] out T value)
        => TinyhandSerializer.TryDeserialize<T>(owner.Memory, out value, SerializerOptions);

    private static class IdCache<T>
    {
        public static readonly uint Id;

        static IdCache()
        {
            try
            {
                var obj = TinyhandSerializer.Reconstruct<T>();
                if (obj is IBlock block)
                {
                    Id = block.BlockId;
                }
            }
            catch
            {
            }
        }
    }
}
