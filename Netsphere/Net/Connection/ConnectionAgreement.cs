﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Netsphere;

[TinyhandObject]
public partial record ConnectionAgreement
{
    public static readonly ConnectionAgreement Default = new();
    internal const ulong DataId = 0x54074a0294a59b25;

    public ConnectionAgreement()
    {
        this.MaxTransmissions = 4;
        this.MaxBlockSize = 4 * 1024 * 1024; // 4MB
        this.MaxStreamLength = 0; // Disabled
        this.StreamBufferSize = 8 * 1024 * 1024; // 8MB
        this.AllowBidirectionalConnection = false;
        this.ConnectionAliveSeconds = 5;
    }

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

    [Key(4)]
    public bool AllowBidirectionalConnection { get; set; }

    [Key(5)]
    public int ConnectionAliveSeconds { get; set; }

    [IgnoreMember]
    public int MaxBlockGenes { get; private set; }

    [IgnoreMember]
    public int StreamBufferGenes { get; private set; }

    private int maxBlockSize;
    private long maxStreamLength;
    private int streamBufferSize;

    public void AcceptAll(ConnectionAgreement target)
    {
        this.MaxTransmissions = Math.Max(this.MaxTransmissions, target.MaxTransmissions);
        this.MaxBlockSize = Math.Max(this.MaxBlockSize, target.MaxBlockSize);

        if (target.MaxStreamLength == -1)
        {
            this.MaxStreamLength = -1;
        }
        else if (target.MaxStreamLength > this.MaxStreamLength)
        {
            this.MaxStreamLength = target.MaxStreamLength;
        }

        this.StreamBufferSize = Math.Max(this.StreamBufferSize, target.StreamBufferSize);
        this.AllowBidirectionalConnection |= target.AllowBidirectionalConnection;
        this.ConnectionAliveSeconds = Math.Max(this.ConnectionAliveSeconds, target.ConnectionAliveSeconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckStreamLength(long maxStreamLength)
    {
        if (this.maxStreamLength < 0)
        {
            return true;
        }

        return this.maxStreamLength >= maxStreamLength;
    }
}
