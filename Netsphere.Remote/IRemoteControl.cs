// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Remote;

[NetServiceInterface]
public interface IRemoteControl : INetService
{
    public NetTask Authenticate(AuthenticationToken token);

    public NetTask<NetResult> Restart();
}
