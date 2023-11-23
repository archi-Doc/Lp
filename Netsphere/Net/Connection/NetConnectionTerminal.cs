// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;

namespace Netsphere;

internal class NetConnectionTerminal
{
    public NetConnectionTerminal(NetStats netStats)
    {
        this.netStats = netStats;
    }

    private readonly NetStats netStats;

    private readonly object syncObject = new();
    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    public ClientConnection? TryConnect(NetAddress address, NetConnection.ConnectMode mode = NetConnection.ConnectMode.ReuseClosed)
    {
        if (!this.netStats.TryCreateEndPoint(in address, out var endPoint))
        {
            return null;
        }

        lock (this.syncObject)
        {
            if (mode == NetConnection.ConnectMode.ReuseOpened)
            {// Attempt to reuse connections that have already been created and are open.
                this.serverConnections.EndPointChain.TryGetValue(endPoint, out var connection);
            }
            else if (mode == NetConnection.ConnectMode.ReuseClosed)
            {// Attempt to reuse connections that have already been closed and are awaiting disposal.
            }
        }

        return default;
    }
}
