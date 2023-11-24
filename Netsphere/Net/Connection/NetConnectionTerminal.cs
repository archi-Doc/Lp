// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;

namespace Netsphere;

internal class NetConnectionTerminal
{// NetConnection: Open(OpenEndPointChain) ->
    public NetConnectionTerminal(NetStats netStats)
    {
        this.netStats = netStats;
    }

    private readonly NetStats netStats;

    private readonly ClientConnection.GoshujinClass clientConnections = new();
    private readonly ServerConnection.GoshujinClass serverConnections = new();

    public ClientConnection? TryConnect(NetAddress address, NetConnection.ConnectMode mode = NetConnection.ConnectMode.ReuseClosed)
    {
        if (!this.netStats.TryCreateEndPoint(in address, out var endPoint))
        {
            return null;
        }

        var systemMics = Mics.GetSystem();
        lock (this.clientConnections.SyncObject)
        {
            if (mode == NetConnection.ConnectMode.ReuseOpen)
            {// Attempt to reuse connections that have already been created and are open.
                if (this.clientConnections.OpenEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    return connection;
                }
            }

            if (mode == NetConnection.ConnectMode.ReuseOpen ||
                mode == NetConnection.ConnectMode.ReuseClosed)
            {// Attempt to reuse connections that have already been closed and are awaiting disposal.
                if (this.clientConnections.ClosedEndPointChain.TryGetValue(endPoint, out var connection))
                {
                    if ((connection.ClosedSystemMics + Mics.FromMinutes(1)) > systemMics)
                    {
                        return connection;
                    }
                }
            }

            // Create a new connection
            var newConnection = new ClientConnection()
        }

        return default;
    }
}
