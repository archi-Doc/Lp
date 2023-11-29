// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Packet;
using Tinyhand.IO;

namespace Netsphere.Transmission;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial class NetGene
{
    public NetGene(uint geneSerial)
    {
        this.GeneSerial = geneSerial;
        // this.GeneMax = geneMax;
    }

    #region FieldAndProperty

    [Link(Type = ChainType.List, AddValue = false)]
    public uint GeneSerial { get; }

    // public uint GeneMax { get; }

    #endregion

    public void SetSend(Connection connection, uint transmissionId, uint geneMax, ByteArrayPool.MemoryOwner block)
    {
        Debug.Assert(block.Span.Length <= GeneFrame.MaxBlockLength);
        var arrayOwner = PacketPool.Rent();

        // PacketHeaderCode
        var span = arrayOwner.ByteArray.AsSpan();

        // Packet
        var salt = RandomVault.Pseudo.NextUInt32();
        BitConverter.TryWriteBytes(span, salt); // Hash
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, connection.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)PacketType.Encrypted); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, connection.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        var encrypt = span;

        // Frame
        BitConverter.TryWriteBytes(span, (ushort)FrameType.Block); // Frame type
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, transmissionId); // TransmissionId
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, this.GeneSerial); // GeneSerial
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, geneMax); // GeneMax
        span = span.Slice(sizeof(uint));

        block.Span.CopyTo(span);
        span = span.Slice(block.Span.Length);

        //connection.Embryo.
    }
}
