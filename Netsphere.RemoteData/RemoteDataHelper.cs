// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace Netsphere.RemoteData;

public static class RemoteDataHelper
{
    public static async Task SendLog(NetTerminal netTerminal, IFileLogger? fileLogger, string netNode, string remotePrivateKey, string identifier)
    {
        if (fileLogger is null ||
            string.IsNullOrEmpty(netNode) ||
            string.IsNullOrEmpty(remotePrivateKey))
        {
            return;
        }

        var r = await NetHelper.TryGetStreamService<IRemoteData>(netTerminal, netNode, remotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        try
        {
            await fileLogger.Flush(false);

            var path = fileLogger.GetCurrentPath();
            using var fileStream = File.OpenRead(path);
            var sendStream = await r.Service.Put(identifier, fileStream.Length);
            if (sendStream is not null)
            {
                var r3 = await NetHelper.StreamToSendStream(fileStream, sendStream);
            }
        }
        catch
        {
        }
        finally
        {
            r.Connection.Dispose();
        }
    }
}
