// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LP;

public static class BlockService
{
    public const int MaxBlockSize = 1024 * 1024 * 4; // 4MB

    public static uint GetId<T>() => IdCache<T>.Id;

    public static bool TrySerialize<T>(T value, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = BlockPool.Rent();
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            TinyhandSerializer.Serialize(ref writer, value);

            writer.FlushAndGetArray(out var array, out var arrayLength);
            if (array != arrayOwner.ByteArray)
            {
                arrayOwner.Return();
                owner = default;
                return false;
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
                    Id = block.Id;
                }
            }
            catch
            {
            }
        }
    }
}
