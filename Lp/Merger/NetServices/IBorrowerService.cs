// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs.NetService;

[NetService]
public interface IBorrowerService : INetService
{
    public Task<T3csResult> Authenticate(Credit credit, AuthenticationToken token);
}
