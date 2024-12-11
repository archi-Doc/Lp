// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject(ReservedKeyCount = 2)]
public partial class CreateRelayBlock
{
    public CreateRelayBlock()
    {
    }

    public CreateRelayBlock(bool allowUnknownNode)
    {
        this.AllowUnknownNode = allowUnknownNode;
    }

    /*public CreateRelayBlock(RelayId relayId)
    {
        this.RelayId = relayId;
    }*/

    /// <summary>
    /// Gets or sets a value indicating whether or not to allow communication from unknown nodes.
    /// </summary>
    [Key(0)]
    public bool AllowUnknownNode { get; protected set; }

    // [Key(1)]
    // public Linkage? Linkage { get; private set; }
}

[TinyhandObject]
public partial class CreateRelayResponse
{
    public CreateRelayResponse()
    {
    }

    public CreateRelayResponse(RelayResult result, RelayId relayId, RelayId outerRelayId, long relayPoint)
    {
        this.Result = result;
        this.RelayId = relayId;
        this.OuterRelayId = outerRelayId;
        this.RelayPoint = relayPoint;
    }

    [Key(0)]
    public RelayResult Result { get; private set; }

    [Key(1)]
    public RelayId RelayId { get; private set; }

    [Key(2)]
    public RelayId OuterRelayId { get; private set; }

    [Key(3)]
    public long RelayPoint { get; private set; }
}
