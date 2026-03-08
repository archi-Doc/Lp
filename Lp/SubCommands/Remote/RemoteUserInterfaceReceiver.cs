// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetObject]
public partial class RemoteUserInterfaceReceiver : IRemoteUserInterfaceReceiver, INetObject
{
    public RemoteUserInterfaceReceiver()
    {
    }

    void INetObject.OnConnectionClosed()
    {
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLine(string message, ConsoleColor color)
    {
        return Task.FromResult(NetResult.Success);
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine()
    {
        throw new NotImplementedException();
    }
}
