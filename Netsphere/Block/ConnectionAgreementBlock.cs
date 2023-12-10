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
    }

    public uint BlockId => 0x12345678;

    [Key(0)]
    public uint MaxTransmissions { get; set; }

    [Key(1)]
    public uint MaxBlockSize
    {
        get => this.maxBlockSize;
        set
        {
            this.maxBlockSize = value;
            var info = NetTransmission.CalculateGene((uint)this.maxBlockSize);
            this.MaxGenes = info.NumberOfGenes;
        }
    }

    [IgnoreMember]
    public uint MaxGenes { get; private set; }

    private uint maxBlockSize;
}
