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
        var seedKey = authority.GetSeedKey();
        if (context.AuthenticationTokenEquals(seedKey.GetSignaturePublicKey()))
        {
            return true;
        }

        var token = AuthenticationToken.CreateAndSign(seedKey, connection);
        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority, Credit credit)
    {
        var context = connection.GetContext();
        var seedKey = authority.GetSeedKey(credit);
        if (context.AuthenticationTokenEquals(seedKey.GetSignaturePublicKey()))
        {
            return true;
        }

        var token = AuthenticationToken.CreateAndSign(seedKey, connection);
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
            logger.GetWriter()?.Write(Hashed.Error.Connect, robustConnection.DestinationNode.ToString());
            return null;
        }

        return connection;
    }
}
