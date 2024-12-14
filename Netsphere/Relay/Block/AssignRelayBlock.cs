// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject(ReservedKeyCount = 2)]
public partial class AssignRelayBlock
{
    public AssignRelayBlock(bool allowUnknownNode = false)
    {
        this.AllowUnknownNode = allowUnknownNode;
        this.InnerKeyAndNonce = new byte[32];
        RandomVault.Default.NextBytes(this.InnerKeyAndNonce);
    }

    protected AssignRelayBlock()
    {
    }

    /*public CreateRelayBlock(RelayId relayId)
    {
        this.RelayId = relayId;
    }*/

    /// <summary>
    /// Gets or sets a value indicating whether or not to allow communication from unknown nodes.<br/>
    /// This feature is designed with Engagement in mind.
    /// </summary>
    [Key(0)]
    public bool AllowUnknownNode { get; protected set; }

    [Key(1)]
    public byte[] InnerKeyAndNonce { get; protected set; } = [];

    // [Key(2)]
    // public Linkage? Linkage { get; private set; }
}

[TinyhandObject]
public partial class AssignRelayResponse
{
    public AssignRelayResponse(RelayResult result, RelayId innerRelayId, RelayId outerRelayId, long relayPoint, long retensionMics)
    {
        this.Result = result;
        this.InnerRelayId = innerRelayId;
        this.OuterRelayId = outerRelayId;
        this.RelayPoint = relayPoint;
        this.RetensionMics = retensionMics;
    }

    protected AssignRelayResponse()
    {
    }

    [Key(0)]
    public RelayResult Result { get; protected set; }

    [Key(1)]
    public RelayId InnerRelayId { get; protected set; }

    [Key(2)]
    public RelayId OuterRelayId { get; protected set; }

    [Key(3)]
    public long RelayPoint { get; protected set; }

    [Key(4)]
    public long RetensionMics { get; protected set; }
}
