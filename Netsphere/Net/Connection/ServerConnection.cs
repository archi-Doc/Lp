// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[ValueLinkObject]
public partial class ServerConnection : NetConnection
{
    [Link(TargetMember = "EndPoint", Type = ChainType.Ordered, AddValue = false)]
    public ServerConnection()
    {
    }
}
