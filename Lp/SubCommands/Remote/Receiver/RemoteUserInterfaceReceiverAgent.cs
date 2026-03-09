// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver, INetObject
{
    public RemoteUserInterfaceReceiverAgent()
    {
    }

    void INetObject.OnConnectionClosed()
    {
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine()
    {
        throw new NotImplementedException();
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description)
    {
        throw new NotImplementedException();
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadPassword(bool cancelOnEscape, string? description)
    {
        throw new NotImplementedException();
    }

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description)
    {
        throw new NotImplementedException();
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        throw new NotImplementedException();
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        throw new NotImplementedException();
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLineDefault(string? message)
    {
        throw new NotImplementedException();
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLineError(string? message)
    {
        throw new NotImplementedException();
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLineWarning(string? message)
    {
        throw new NotImplementedException();
    }
}
