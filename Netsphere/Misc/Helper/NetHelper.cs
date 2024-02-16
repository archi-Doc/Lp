// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere;

public static class NetHelper
{
    internal const int RamaGenes = 3;
    internal const char Quote = '\"';
    internal const string TripleQuotes = "\"\"\"";

    public static ulong GetDataId<TSend, TReceive>()
        => (ulong)Tinyhand.TinyhandHelper.GetFullNameId<TSend>() | ((ulong)Tinyhand.TinyhandHelper.GetFullNameId<TReceive>() << 32);

    public static string To4Hex(this ulong gene) => $"{(ushort)gene:x4}";

    public static string To4Hex(this uint id) => $"{(ushort)id:x4}";

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int NumberOfGenes, uint FirstGeneSize, uint LastGeneSize) CalculateGene(long size)
    {// FirstGeneSize, GeneFrame.MaxBlockLength..., LastGeneSize
        if (size <= FirstGeneFrame.MaxGeneLength)
        {
            return (1, (uint)size, 0);
        }

        size -= FirstGeneFrame.MaxGeneLength;
        var numberOfGenes = (int)(size / FollowingGeneFrame.MaxGeneLength);
        var lastGeneSize = (uint)(size - (numberOfGenes * FollowingGeneFrame.MaxGeneLength));
        return (lastGeneSize > 0 ? numberOfGenes + 2 : numberOfGenes + 1, FirstGeneFrame.MaxGeneLength, lastGeneSize);
    }
}
