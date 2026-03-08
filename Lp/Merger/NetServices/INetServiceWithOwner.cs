// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public interface INetServiceWithOwner : INetService
{
    Task<NetResult> Authenticate(OwnerToken token);
}
