// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;
using Netsphere.Server;

namespace Netsphere.Block;

[TinyhandObject]
public partial class ConnectionAgreementBlock : IBlock
{
    public static readonly ConnectionAgreementBlock Default = new();

    public ConnectionAgreementBlock()
    {
    }

    public ConnectionAgreementBlock(ServerOptions options)
    {
        this.MaxTransmissions = options.MaxTransmissions;
        this.MaxBlockSize = options.MaxBlockSize;
        this.MaxStreamSize = options.MaxStreamSize;
    }

    public uint BlockId => 0x12345678;

    [Key(0)]
    public uint MaxTransmissions { get; set; }

    [Key(1)]
    public int MaxBlockSize
    {
        get => this.maxBlockSize;
        set
        {
            this.maxBlockSize = value;
            var info = NetTransmission.CalculateGene(this.maxBlockSize);
            this.MaxBlockGenes = info.NumberOfGenes;
        }
    }

    [Key(2)]
    public long MaxStreamSize
    {
        get => this.maxStreamSize;
        set
        {
            this.maxStreamSize = value;
            var info = NetTransmission.CalculateGene(this.maxStreamSize);
            this.MaxStreamGenes = info.NumberOfGenes;
        }
    }

    [IgnoreMember]
    public uint MaxBlockGenes { get; private set; }

    [IgnoreMember]
    public uint MaxStreamGenes { get; private set; }

    private int maxBlockSize;
    private long maxStreamSize;
}
