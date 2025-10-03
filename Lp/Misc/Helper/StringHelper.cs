// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc;
using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand;
using Tinyhand.IO;

namespace Lp;

public static class StringHelper
{
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
        var span = input.AsSpan();
        Span<char> dest = stackalloc char[input.Length];

        // Remove control characters.
        var destLength = 0;
        foreach (var x in span)
        {
            if (!char.IsControl(x))
            {
                dest[destLength++] = x;
            }
        }

        // Leading white-space
        var start = 0;
        for (start = 0; start < destLength; start++)
        {
            if (!char.IsWhiteSpace(dest[start]))
            {
                break;
            }
        }

        // Trailing white-space
        var end = destLength - 1;
        for (; start < end; end--)
        {
            if (!char.IsWhiteSpace(dest[end]))
            {
                break;
            }
        }

        if (start == 0 && end == (input.Length - 1))
        {// Returns the original string.
            return input;
        }
        else
        {// Cleaned string.
            return dest.Slice(start, end - start + 1).ToString();
        }
    }
}
