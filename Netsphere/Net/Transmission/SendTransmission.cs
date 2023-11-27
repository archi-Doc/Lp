// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Transmission;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class SendTransmission
{
    [Link(Primary = true, Name = "SendQueue", Type = ChainType.QueueList, AutoLink = false)]
    public SendTransmission()
    {
    }
}
