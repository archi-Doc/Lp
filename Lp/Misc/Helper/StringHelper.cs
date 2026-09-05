// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public static class StringHelper
{
    public static (char[] Rent, int Length) AppendPrefix(ReadOnlySpan<char> prefix, ReadOnlySpan<char> message)
    {
        if (message.IsEmpty)
        {
            return ([], 0);
        }

        var source = message;
        var prefixCount = 1 + source.Count('\n');
        var maxLength = checked(message.Length + (prefix.Length * prefixCount));
        var rent = ArrayPool<char>.Shared.Rent(maxLength);
        var destination = rent.AsSpan();

        prefix.CopyTo(destination);
        destination = destination.Slice(prefix.Length);
        foreach (var x in source)
        {
            if (x == '\r')
            {
            }
            else
            {
                destination[0] = x;
                destination = destination.Slice(1);
                if (x == '\n')
                {
                    prefix.CopyTo(destination);
                    destination = destination.Slice(prefix.Length);
                }
            }
        }

        return (rent, rent.Length - destination.Length);
    }

    public static string SerializeToString<T>(T value)
    {
        return TinyhandSerializer.SerializeToString<T>(value, TinyhandSerializerOptions.ConvertToStrictString);
    }

    public static T? DeserializeFromString<T>(string utf16)
    {
        try
        {
            return TinyhandSerializer.DeserializeFromString<T>(utf16, TinyhandSerializerOptions.ConvertToStrictString);
        }
        catch
        {
            return default;
        }
    }

    public static string UnwrapQuote(this string input)
    {
        if (input.Length >= 2 && input[0] == '\'' && input[^1] == '\'')
        {
            return input.Substring(1, input.Length - 2);
        }

        return input;
    }

    public static string ToMergerString(this SignaturePublicKey[] mergers, IConversionOptions? conversionOptions)
    {
        Span<char> buffer = stackalloc char[Credit.MaxStringLength];
        var span = buffer;

        var written = 0;
        var isFirst = true;
        foreach (var x in mergers)
        {
            if (isFirst)
            {
                isFirst = false;
                span[0] = LpConstants.MergerSymbol;
                span = span.Slice(1);
                written += 1;
            }
            else
            {
                span[0] = LpConstants.MergerSeparatorSymbol;
                span = span.Slice(1);
                written += 1;
            }

            if (!x.TryFormat(span, out var w, conversionOptions))
            {
                return string.Empty;
            }

            span = span.Slice(w);
            written += w;
        }

        return buffer.Slice(0, written).ToString();
    }

    /// <summary>
    /// Removes control characters and leading white-space and trailing white-space.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The result.</returns>
    public static string CleanupInput(this string input)
    {
        if (input.Length <= 256)
        {
            Span<char> buffer = stackalloc char[input.Length];
            var written = 0;
            foreach (var c in input)
            {
                if (!char.IsControl(c))
                {
                    buffer[written++] = c;
                }
            }

            var first = 0;
            while (first < written && char.IsWhiteSpace(buffer[first]))
            {
                first++;
            }

            var last = written - 1;
            while (first < last && char.IsWhiteSpace(buffer[last]))
            {
                last--;
            }

            return first == 0 && last == input.Length - 1 ? input : buffer.Slice(first, last - first + 1).ToString();
        }

        // Leading white-space
        var start = 0;
        while (start < input.Length && (char.IsWhiteSpace(input[start]) || char.IsControl(input[start])))
        {
            start++;
        }

        // Trailing white-space
        var end = input.Length;
        while (start < end && (char.IsWhiteSpace(input[end - 1]) || char.IsControl(input[end - 1])))
        {
            end--;
        }

        // Remove control characters.
        var length = end - start;
        for (var i = start; i < end; i++)
        {
            if (char.IsControl(input[i]))
            {
                length--;
            }
        }

        if (length == input.Length)
        {// Returns the original string.
            return input;
        }

        if (length == end - start)
        {
            return input.Substring(start, length);
        }

        return string.Create(length, (Input: input, Start: start, End: end), static (destination, state) =>
        {// Cleaned string.
            var written = 0;
            for (var i = state.Start; i < state.End; i++)
            {
                var c = state.Input[i];
                if (!char.IsControl(c))
                {
                    destination[written++] = c;
                }
            }
        });
    }
}
