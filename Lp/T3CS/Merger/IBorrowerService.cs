// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS.NetService;

[NetServiceInterface]
public interface IBorrowerService : INetService
{
    public NetTask<T3csResult> Authenticate(Credit credit, AuthenticationToken token);
}
