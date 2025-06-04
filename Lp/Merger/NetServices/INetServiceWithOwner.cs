// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public interface INetServiceWithOwner : INetService
{
    NetTask<NetResult> Authenticate(OwnerToken token);
}
