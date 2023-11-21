// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;

namespace Netsphere;

internal class NetConnectionControl
{
    public NetConnectionControl(NetStats netStats)
    {
        this.netStats = netStats;
    }

    private readonly NetStats netStats;

    private readonly object syncObject = new();
    private readonly ServerConnection.GoshujinClass closedServerConnections = new();

    public ClientConnection? TryConnect(NetAddress address, NetConnection.ConnectMode mode = NetConnection.ConnectMode.ReuseClosed)
    {
        this.netStats.TryCreateEndPoint(in address, out var endPoint);
        if (!endPoint.IsValid)
        {
            return null;
        }

        lock (this.syncObject)
        {
            if (mode == NetConnection.ConnectMode.ReuseOpened)
            {// Attempt to reuse connections that have already been created and are open.
                this.closedServerConnections.EndPointChain.TryGetValue(endPoint, out var connection);
            }
            else if (mode == NetConnection.ConnectMode.ReuseClosed)
            {// Attempt to reuse connections that have already been closed and are awaiting disposal.
            }
        }

        return default;
    }
}
