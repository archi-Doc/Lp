// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Subcommands;

internal static class LpDogmaHelper
{
    public static async Task<ConnectionAndService<LpDogmaNetService>> TryConnect(ILogger logger, AuthorityControl authorityControl, NetTerminal netTerminal, string netNode)
    {
        if (await authorityControl.GetLpSeedKey(logger) is not { } lpSeedKey)
        {
            return default;
        }

        if (!NetNode.TryParseNetNode(logger, netNode, out var node))
        {
            return default;
        }

        var connection = await netTerminal.Connect(node);
        if (connection is null)
        {
            logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, netNode.ToString());
            return default;
        }

        var service = connection.GetService<LpDogmaNetService>();
        var auth = AuthenticationToken.CreateAndSign(lpSeedKey, connection);
        var r = await service.Authenticate(auth);
        if (r.Result.IsError())
        {
            connection.Dispose();
            return default;
        }

        return new(connection, service);
    }
}
