// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Tinyhand.Logging;

namespace Netsphere;

/// <summary>
/// RobustConnection is designed to maintain a limited number of connections and is not intended for a large number of connections.<br/>
/// If the number exceeds <see cref="MaxConnections"/>, it will be reinitialized.
/// </summary>
public class RobustConnection
{
    public const int MaxConnections = 100;

    public class Terminal
    {
        public Terminal(NetTerminal netTerminal)
        {
            this.netTerminal = netTerminal;
        }

        private readonly NetTerminal netTerminal;
        private readonly NotThreadsafeHashtable<NetNode, RobustConnection> connections = new();

        public RobustConnection Open(NetNode node, Options? options = default)
        {
            var robustConnection = this.connections.GetOrAdd(node, x => new RobustConnection(this.netTerminal, x));
            if (options is not null)
            {
                robustConnection.TrySetOptions(options);
            }

            return robustConnection;
        }

        public void Clean()
        {
            if (this.connections.Count > MaxConnections)
            {
                this.connections.Clear();
            }
        }
    }

    public record class Options(SignaturePrivateKey? PrivateKey);

    #region FieldAndProperty

    private readonly NetTerminal netTerminal;
    private readonly NetNode netNode;
    private readonly SemaphoreLock semaphore = new();
    private Options? options;
    private ClientConnection? connection;

    #endregion

    private RobustConnection(NetTerminal netTerminal, NetNode netNode)
    {
        this.netTerminal = netTerminal;
        this.netNode = netNode;
    }

    public async ValueTask<ClientConnection?> Get()
    {
        var currentConnection = this.connection; // Since it is outside the lock statement, the reference to connection is not safe.
        if (currentConnection?.IsActive == true)
        {
            return currentConnection;
        }

        ClientConnection? newConnection = default;
        await this.semaphore.EnterAsync().ConfigureAwait(false);
        try
        {
            if (this.connection?.IsActive == true)
            {// Safe
                return this.connection;
            }

            if (this.connection is not null)
            {
                this.connection.Dispose();
                this.connection = null;
            }

            newConnection = await this.netTerminal.Connect(this.netNode, Connection.ConnectMode.NoReuse).ConfigureAwait(false);
            if (newConnection is null)
            {// Failed to connect
                return default;
            }

            if (this.options?.PrivateKey is { } privateKey)
            {
                var context = newConnection.GetContext();
                var token = new AuthenticationToken(newConnection.Salt);
                token.Sign(privateKey);
                if (!context.AuthenticationTokenEquals(token.PublicKey))
                {
                    var result = await newConnection.SetAuthenticationToken(token).ConfigureAwait(false);
                    if (result != NetResult.Success)
                    {
                        this.options = default;//
                        return default;
                    }
                }
            }

            this.connection = newConnection;
        }
        finally
        {
            this.semaphore.Exit();
        }

        return newConnection;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrySetOptions(Options options)
    {
        // Interlocked.CompareExchange(ref this.options, options, null);
        this.options ??= options;
    }
}
