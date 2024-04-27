// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject(ReservedKeys = 2)]
public partial class CreateRelayBlock
{
    public CreateRelayBlock()
    {
    }

    /*public CreateRelayBlock(ushort relayId)
    {
        this.RelayId = relayId;
    }*/

    // [Key(0)]
    // public ushort RelayId { get; protected set; }

    // [Key(1)]
    // public Linkage? Linkage { get; private set; }
}

[TinyhandObject]
public partial class CreateRelayResponse
{
    public CreateRelayResponse()
    {
    }

    public CreateRelayResponse(RelayResult result, ushort relayId)
    {
        this.Result = result;
        this.RelayId = relayId;
    }

    [Key(0)]
    public RelayResult Result { get; private set; }

    [Key(1)]
    public ushort RelayId { get; private set; }

    [Key(2)]
    public long RelayPoint { get; private set; }
}
