// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Transmission;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class SendTransmission : Transmission
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "TransmissionId", AddValue = false)]
    [Link(Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public SendTransmission(uint transmissionId)
        : base(transmissionId)
    {
    }
}
