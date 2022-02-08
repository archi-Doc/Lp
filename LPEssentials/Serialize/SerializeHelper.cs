// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public static class SerializeHelper
{
    public static byte[] Serialize(Dictionary<ulong, ILPSerializable> dictionary)
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

    public static bool Deserialize(Dictionary<ulong, ILPSerializable> dictionary, ReadOnlyMemory<byte> memory)
    {
        try
        {
            while (memory.Length >= 12)
            {
                var id = BitConverter.ToUInt64(memory.Span); // Id
                memory = memory.Slice(8);
                var length = BitConverter.ToInt32(memory.Span); // Length
                memory = memory.Slice(4);

                if (dictionary.TryGetValue(id, out var x))
                {
                    x.Deserialize(memory, out _);
                }

                memory = memory.Slice(length);
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
}
