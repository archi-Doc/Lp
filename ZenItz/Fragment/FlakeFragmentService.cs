// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public static class FlakeFragmentService
{
    public const int StandardFragmentSize = 1024 * 2; // 2KB

    public static bool TrySerialize<T>(T obj, out ByteArrayPool.MemoryOwner owner)
        where T : ITinyhandSerialize<T>
    {
        var arrayOwner = BlockPool.Rent(StandardFragmentSize);
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            TinyhandSerializer.SerializeObject(ref writer, obj, SerializerOptions);

            writer.FlushAndGetArray(out var array, out var arrayLength);
            if (array != arrayOwner.ByteArray)
            {
                arrayOwner.Return();
                owner = new ByteArrayPool.MemoryOwner(array);
                return true;
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

    /*public static bool TrySerialize(this ITinyhandSerialize obj, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = BlockPool.Rent(StandardFragmentSize);
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            obj.Serialize(ref writer, SerializerOptions);

            writer.FlushAndGetArray(out var array, out var arrayLength);
            if (array != arrayOwner.ByteArray)
            {
                arrayOwner.Return();
                if (arrayLength > ZenOptions.DefaultMaxDataSize)
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
    }*/

    /*public static bool TrySerialize<T>(T value, out ByteArrayPool.MemoryOwner owner)
    {
        var arrayOwner = BlockPool.Rent(StandardFragmentSize);
        try
        {
            var writer = new Tinyhand.IO.TinyhandWriter(arrayOwner.ByteArray);
            TinyhandSerializer.Serialize(ref writer, value, SerializerOptions);

            writer.FlushAndGetArray(out var array, out var arrayLength);
            if (array != arrayOwner.ByteArray)
            {
                arrayOwner.Return();
                if (arrayLength > ZenOptions.DefaultMaxDataSize)
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
    }*/

    public static TinyhandSerializerOptions SerializerOptions { get; } = TinyhandSerializerOptions.Standard;
}
