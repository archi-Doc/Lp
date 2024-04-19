// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Packet;

[TinyhandObject(ReservedKeys = 2)]
public partial class CreateRelayBlock
{
    internal const ulong DataId = 0x4CB0F32920D365F6;

    public CreateRelayBlock()
    {
    }

    public CreateRelayBlock(ushort relayId)
    {
        this.RelayId = relayId;
    }

    [Key(0)]
    public ushort RelayId { get; protected set; }

    // [Key(1)]
    // public Linkage? Linkage { get; private set; }
}

[TinyhandObject]
public sealed partial class CreateRelayResponse
{
    public CreateRelayResponse()
    {
    }

    public CreateRelayResponse(RelayResult result)
    {
        this.Result = result;
    }

    [Key(0)]
    public RelayResult Result { get; private set; }

    [Key(1)]
    public long RelayPoint { get; private set; }
}
