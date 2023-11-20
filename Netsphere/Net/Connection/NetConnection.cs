// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[ValueLinkObject]
public partial class NetConnection
{
    public enum ConnectMode
    {
        ReuseClosed,
        ReuseOpened,
        NoReuse,
    }

    public NetConnection()
    {
    }

    [Link(Type = ChainType.Unordered, AddValue = false)]
    public NetEndPoint NetEndPoint { get; }
}
