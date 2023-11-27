// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Block;

[TinyhandObject]
public partial class ConnectionRequirementsBlock : IBlock
{
    public uint BlockId => 0x12345678;

    [Key(0)]
    public int MaxTransmissions { get; set; }

    [Key(1)]
    public int TransmissionWindow { get; set; }

}
