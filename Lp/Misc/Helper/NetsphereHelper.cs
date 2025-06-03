// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public static class NetsphereHelper
{
    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority)
    {
        var context = connection.GetContext();
        var token = AuthenticationToken.CreateAndSign(authority.GetSeedKey(), connection);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority, Credit credit)
    {
        var context = connection.GetContext();
        var token = AuthenticationToken.CreateAndSign(authority.GetSeedKey(credit), connection);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static async ValueTask<ClientConnection?> Get(this RobustConnection? robustConnection, ILogger logger)
    {
        if (robustConnection is null)
        {
            return null;
        }

        if (await robustConnection.Get() is not { } connection)
        {
            logger.TryGet()?.Log(Hashed.Error.Connect, robustConnection.DestinationNode.ToString());
            return null;
        }

        return connection;
    }
}
