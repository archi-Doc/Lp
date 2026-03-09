// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceSender : INetServiceWithConnectBidirectionally
{
    Task<NetResult> Send(string message);
}
