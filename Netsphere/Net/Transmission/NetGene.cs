// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Packet;
using Tinyhand.IO;

namespace Netsphere.Transmission;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial class NetGene
{
    [Link(Type = ChainType.SlidingList, Name = "SlidingList", AddValue = false)]
    public NetGene()
    {
        // this.GeneSerial = geneSerial;
        // this.GeneMax = geneMax;
    }

    #region FieldAndProperty

    public int GeneSerial => this.Goshujin is null ? 0 : this.SlidingListLink.Position;

    // public uint GeneMax { get; }

    #endregion

    public void SetSend(ByteArrayPool.MemoryOwner toBeShared)
    {
    }
}
