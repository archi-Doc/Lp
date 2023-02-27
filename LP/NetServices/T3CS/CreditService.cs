// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere;

namespace LP.NetServices.T3CS;

[NetServiceInterface]
public interface ICreditService : INetService
{
    public NetTask<MergerResult> Open(Credit credit);
}
