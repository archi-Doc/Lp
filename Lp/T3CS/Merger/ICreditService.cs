// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;

namespace Lp.T3cs.NetService;

[NetServiceInterface]
public interface ICreditService : INetService
{
    public NetTask<T3csResult> Open(Credit credit);
}
