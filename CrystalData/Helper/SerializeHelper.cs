// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ISimpleSerializable
{
    void Serialize(ref Tinyhand.IO.TinyhandWriter writer);

    bool Deserialize(ReadOnlySpan<byte> span, out int bytesRead);
}

internal static class SerializeHelper
{
    public const int StandardFragmentSize = 1024 * 2; // 2KB

    public static TinyhandSerializerOptions SerializerOptions { get; } = TinyhandSerializerOptions.Standard;

    public static bool TrySerialize<T>(T obj, out ByteArrayPool.MemoryOwner owner)
        where T : ITinyhandSerialize<T>
    {
        var arrayOwner = ByteArrayPool.Default.Rent(StandardFragmentSize);
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

    public static byte[] Serialize(Dictionary<ulong, ISimpleSerializable> dictionary)
    {
        var writer = default(Tinyhand.IO.TinyhandWriter);
        byte[]? byteArray;
        try
        {
            foreach (var x in dictionary)
            {
                var span = writer.GetSpan(12); // Id + Length
                writer.Advance(12);

                var written = writer.Written;
                x.Value.Serialize(ref writer);

                BitConverter.TryWriteBytes(span, x.Key); // Id
                span = span.Slice(8);
                BitConverter.TryWriteBytes(span, (int)(writer.Written - written)); // Length
            }

            byteArray = writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }

        return byteArray;
    }

    public static bool Deserialize(Dictionary<ulong, ISimpleSerializable> dictionary, ReadOnlySpan<byte> span)
    {
        try
        {
            while (span.Length >= 12)
            {
                var id = BitConverter.ToUInt64(span); // Id
                span = span.Slice(8);
                var length = BitConverter.ToInt32(span); // Length
                span = span.Slice(4);

                if (dictionary.TryGetValue(id, out var x))
                {
                    x.Deserialize(span, out _);
                }

                span = span.Slice(length);
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
}
