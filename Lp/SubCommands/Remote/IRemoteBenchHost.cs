// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetServiceInterface]
public interface IRemoteUserInterfaceClient : INetServiceWithConnectBidirectionally
{// INetServiceWithUpdateAgreement
    Task<NetResult> Send(string message);
}

[NetServiceInterface]
public interface IRemoteUserInterfaceServer : INetService
{
    NetTask<ulong> GetHash(byte[] data);
}
