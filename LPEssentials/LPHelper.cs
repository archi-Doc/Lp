// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public static partial class LPHelper
{
    internal const char Quote = '\"';
    internal const string TripleQuotes = "\"\"\"";

    public static string TrimQuotes(this string text)
    {
        if (text.Length >= 6 && text.StartsWith(TripleQuotes) && text.EndsWith(TripleQuotes))
        {
            return text.Substring(3, text.Length - 6);
        }
        else if (text.Length >= 2 && text.StartsWith(Quote) && text.EndsWith(Quote))
        {
            return text.Substring(1, text.Length - 2);
        }

        return text;
    }
}
