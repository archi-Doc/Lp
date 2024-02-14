// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Netsphere.Misc;

internal static class TokenHelper
{
    public static bool TryParse<T>(char identifier, ReadOnlySpan<char> source, [MaybeNullWhen(false)] out T instance)
        where T : ITinyhandSerialize<T>
    {
        instance = default;
        if (source.Length < 3)
        {
            return false;
        }
        else if (source[0] != '{' || source[1] != identifier || source[source.Length - 1] != '}')
        {
            return false;
        }

        source = source.Slice(2, source.Length - 3);
        var decodedLength = Base64.Url.GetMaxDecodedLength(source.Length);

        byte[]? rent = null;
        Span<byte> span = decodedLength <= 4096 ?
            stackalloc byte[decodedLength] : (rent = ArrayPool<byte>.Shared.Rent(decodedLength));

        var result = Base64.Url.FromStringToSpan(source, span, out var written);
        if (rent != null)
        {
            ArrayPool<byte>.Shared.Return(rent);
        }

        if (!result)
        {
            return false;
        }

        try
        {
            instance = TinyhandSerializer.DeserializeObject<T>(span);
        }
        catch
        {
            return false;
        }

        return instance is not null;
    }

    public static bool TryFormat<T>(T value, char identifier, Span<char> destination, out int written)
        where T : ITinyhandSerialize<T>
    {
        written = 0;
        var b = TinyhandSerializer.SerializeObject(value);
        var length = 3 + Base64.Url.GetEncodedLength(b.Length);

        if (destination.Length < length)
        {
            return false;
        }

        var span = destination.Slice(2);
        if (!Base64.Url.FromByteArrayToSpan(b, span, out var w))
        {
            return false;
        }

        destination[0] = '{';
        destination[1] = identifier;
        span = span.Slice(w);
        span[0] = '}';

        written = 3 + w;
        return true;
    }

    public static string ToBase64<T>(T value, char identifier)
        where T : ITinyhandSerialize<T>
    {
        return "{" + identifier + Base64.Url.FromByteArrayToString(TinyhandSerializer.SerializeObject(value)) + "}";
    }
}
