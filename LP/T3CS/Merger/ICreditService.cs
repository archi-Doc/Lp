// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere;

namespace LP.T3CS.NetService;

[NetServiceInterface]
public interface ICreditService : INetService
{
    public NetTask<T3csResult> Open(Credit credit);
}
