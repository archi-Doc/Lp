// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using System.Collections.Generic;

namespace Netsphere;

public class RobustConnection
{
    public class Factory
    {
        public Factory(NetTerminal netTerminal)
        {
            this.netTerminal = netTerminal;
        }

        private readonly NetTerminal netTerminal;

        public async Task<RobustConnection?> Create(NetNode node, ILogger? logger)
        {
            // Connect
            var connection = await this.netTerminal.Connect(node, Connection.ConnectMode.ReuseIfAvailable);
            if (connection is null)
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, node.ToString());
                return default;
            }

            var context = connection.GetContext();
            var token = new AuthenticationToken(connection.Salt);
            authority.Sign(token);
            if (!context.AuthenticationTokenEquals(token.PublicKey))
            {
                var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
                if (result != NetResult.Success)
                {
                    logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
                    return null;
                }
            }

            return default;
        }
    }

    private RobustConnection()
    {
    }
}
