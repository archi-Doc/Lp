// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public static class LpHelper
{
    public static async ValueTask<ClientConnection?> GetConnection(this RobustConnection? robustConnection, ILogger logger)
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
