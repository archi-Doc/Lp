// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
internal partial class AckBuffer
{
    [Link(Name = "ConnectionId", Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    public AckBuffer(Connection connection)
    {
        this.Connection = connection;
        this.AckTime = Mics.GetSystem() + NetConstants.AckDelayMics;
    }

    public Connection Connection { get; }

    public ulong ConnectionId
        => this.Connection.ConnectionId;

    [Link(Type = ChainType.Ordered)]
    public long AckTime { get; set; }
}
