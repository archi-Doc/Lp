// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

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

    public NetEndPoint EndPoint { get; }
}
