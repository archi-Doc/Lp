// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver, INetObject
{
    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;

    internal RemoteUserInterfaceReceiverAgent(ConsoleUserInterfaceService consoleUserInterfaceService)
    {
        this.consoleUserInterfaceService = consoleUserInterfaceService;
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
        his.consoleUserInterfaceService.Write(message, color);
        return Task.FromResult(NetResult.Success);
    }

    Task<NetResult> IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.WriteLine(message, color);
        return Task.FromResult(NetResult.Success);
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
