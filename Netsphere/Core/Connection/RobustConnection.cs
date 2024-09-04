// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

/// <summary>
/// RobustConnection is designed to maintain a limited number of connections and is not intended for a large number of connections.<br/>
/// If the number exceeds <see cref="MaxConnections"/>, it will be reinitialized.
/// </summary>
public class RobustConnection
{
    public const int MaxConnections = 1_000;

    public class Terminal
    {
        public Terminal(NetTerminal netTerminal)
        {
            this.netTerminal = netTerminal;
        }

        private readonly NetTerminal netTerminal;
        private readonly NotThreadsafeHashtable<NetNode, RobustConnection> connections = new();

        public RobustConnection Open(NetNode node)
        {
            return this.connections.GetOrAdd(node, x => new RobustConnection(this.netTerminal, x));
        }

        public void Clean()
        {
            if (this.connections.Count > MaxConnections)
            {
                this.connections.Clear();
            }
        }
    }

    public record class Options();

    #region FieldAndProperty

    private readonly NetTerminal netTerminal;
    private readonly NetNode netNode;
    private readonly SemaphoreLock semaphore = new();
    private ClientConnection? connection;

    #endregion

    private RobustConnection(NetTerminal netTerminal, NetNode netNode)
    {
        this.netTerminal = netTerminal;
        this.netNode = netNode;
    }

    public async ValueTask<ClientConnection?> Get()
    {
        if (this.connection?.IsActive == true)
        {
            return this.connection;
        }

        await this.semaphore.EnterAsync().ConfigureAwait(false);
        try
        {
            if (this.connection?.IsActive == true)
            {
                return this.connection;
            }

            if (this.connection is not null)
            {
                this.connection.Dispose();
                this.connection = null;
            }

            var newConnection = await this.netTerminal.Connect(this.netNode, Connection.ConnectMode.NoReuse).ConfigureAwait(false);
            if (newConnection is null)
            {// Failed to connect
                return default;
            }

            this.connection = newConnection;
        }
        finally
        {
            this.semaphore.Exit();
        }

        return default;
    }
}
