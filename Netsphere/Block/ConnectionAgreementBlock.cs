// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;
using Netsphere.Server;

namespace Netsphere.Block;

[TinyhandObject]
public partial record ConnectionAgreementBlock : IBlock
{
    public static readonly ConnectionAgreementBlock Default = new();
    internal const ulong DataId = 0x54074a0294a59b25;

    public ConnectionAgreementBlock()
    {
    }

    public ConnectionAgreementBlock(ServerOptions options)
    {
        this.MaxTransmissions = options.MaxTransmissions;
        this.MaxBlockSize = options.MaxBlockSize;
        this.MaxStreamLength = options.MaxStreamLength;
        this.StreamBufferSize = options.StreamBufferSize;
    }

    public uint BlockId => 0x95843fb5;

    [Key(0)]
    public uint MaxTransmissions { get; set; }

    [Key(1)]
    public int MaxBlockSize
    {
        get => this.maxBlockSize;
        set
        {
            this.maxBlockSize = value;
            var info = NetHelper.CalculateGene(this.maxBlockSize);
            this.MaxBlockGenes = info.NumberOfGenes;
        }
    }

    [Key(2)]
    public long MaxStreamLength
    {
        get => this.maxStreamLength;
        set
        {
            this.maxStreamLength = value;
            var info = NetHelper.CalculateGene(this.maxStreamLength);
            // this.MaxStreamGenes = info.NumberOfGenes;
        }
    }

    [Key(3)]
    public int StreamBufferSize
    {
        get => this.streamBufferSize;
        set
        {
            this.streamBufferSize = value;
            var info = NetHelper.CalculateGene(this.streamBufferSize);
            this.StreamBufferGenes = info.NumberOfGenes;
        }
    }

    [IgnoreMember]
    public int MaxBlockGenes { get; private set; }

    /*[IgnoreMember]
    public int MaxStreamGenes { get; private set; }*/

    [IgnoreMember]
    public int StreamBufferGenes { get; private set; }

    private int maxBlockSize;
    private long maxStreamLength;
    private int streamBufferSize;

    public void Update(ConnectionAgreementBlock target)
    {
        if (target.MaxTransmissions > this.MaxTransmissions)
        {
            this.MaxTransmissions = target.MaxTransmissions;
        }

        if (target.MaxBlockSize > this.MaxBlockSize)
        {
            this.MaxBlockSize = target.MaxBlockSize;
        }

        if (target.MaxStreamLength == -1)
        {
            this.MaxStreamLength = -1;
        }
        else if (target.MaxStreamLength > this.MaxStreamLength)
        {
            this.MaxStreamLength = target.MaxStreamLength;
        }

        if (target.StreamBufferSize > this.StreamBufferSize)
        {
            this.StreamBufferSize = target.StreamBufferSize;
        }
    }
}
