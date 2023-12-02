// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class SendTransmission : Transmission
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "TransmissionId", AddValue = false, Accessibility = ValueLinkAccessibility.Private)]
    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false, Accessibility = ValueLinkAccessibility.Private)]
    public SendTransmission(Connection connection, uint transmissionId)
        : base(connection, transmissionId)
    {
    }

    private readonly object syncObject = new();
    private NetGene? sendGene; // Single gene
    private NetGene.GoshujinClass? sendGenes; // Multiple genes

    internal NetResult SendBlock(uint blockType, ulong blockId, ByteArrayPool.MemoryOwner block)
    {
        if (this.sendGene is not null || this.sendGenes is not null)
        {
            return NetResult.TransmissionConsumed;
        }

        var size = sizeof(uint) + sizeof(ulong) + block.Span.Length;
        var info = CalculateGene(size);

        lock (this.syncObject)
        {
            if (info.NumberOfGenes == 1)
            {// Single gene
                this.sendGene = new();
                if (this.CreatePacket(info.NumberOfGenes, this.sendGene, block.Span, out var owner))
                {
                    this.sendGene.SetSend(owner);
                }
            }
            else
            {// Multiple genes
            }
        }

        if (info.NumberOfGenes > FlowTerminal.GeneThreshold)
        {// Flow control
        }
        else
        {
            this.Connection.ConnectionTerminal.AddSend(this);
        }

        return NetResult.Success;
    }

    internal async Task<NetResponseData> ReceiveBlock()
    {
        return new();
    }
}
