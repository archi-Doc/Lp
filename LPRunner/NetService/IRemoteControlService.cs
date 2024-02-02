// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;

namespace LP.NetServices;

[NetServiceInterface]
public interface IRemoteControlService : INetService
{
    public NetTask Authenticate(AuthenticationToken token);

    public NetTask<NetResult> Restart();
}
