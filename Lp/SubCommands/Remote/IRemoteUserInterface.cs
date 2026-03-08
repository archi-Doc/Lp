// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetServiceInterface]
public interface IRemoteUserInterfaceSender : INetServiceWithConnectBidirectionally
{// INetServiceWithUpdateAgreement
    Task<NetResult> Send(string message);
}

[NetServiceInterface]
public interface IRemoteUserInterfaceReceiver : INetService
{
    Task<NetResult> WriteLine(string message, ConsoleColor color);

    Task<InputResult> ReadLine();
}
