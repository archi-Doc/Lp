// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.T3CS;

[NetServiceInterface]
public interface ICreditService : INetService
{
    public NetTask<MergerResult> Open(Credit credit);
}
