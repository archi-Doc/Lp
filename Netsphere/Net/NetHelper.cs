// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Packet;

namespace Netsphere.Net;

internal class NetHelper
{
    internal const int RamaGenes = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (uint NumberOfGenes, uint FirstGeneSize, uint LastGeneSize) CalculateGene(long size)
    {// FirstGeneSize, GeneFrame.MaxBlockLength..., LastGeneSize
        if (size <= FirstGeneFrame.MaxGeneLength)
        {
            return (1, (uint)size, 0);
        }

        size -= FirstGeneFrame.MaxGeneLength;
        var numberOfGenes = (uint)(size / FollowingGeneFrame.MaxGeneLength);
        var lastGeneSize = (uint)(size - (numberOfGenes * FollowingGeneFrame.MaxGeneLength));
        return (FirstGeneFrame.MaxGeneLength, lastGeneSize > 0 ? numberOfGenes + 2 : numberOfGenes + 1, lastGeneSize);
    }
}
