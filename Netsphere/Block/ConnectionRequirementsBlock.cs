// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        this.TransmissionWindow = options.TransmissionWindow;
    }

    public uint BlockId => 0x12345678;

    [Key(0)]
    public int MaxTransmissions { get; set; } = -1;

    [Key(1)]
    public int TransmissionWindow { get; set; } = -1;
}
