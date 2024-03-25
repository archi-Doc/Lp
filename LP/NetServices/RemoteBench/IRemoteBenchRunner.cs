﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.NetServices;

[NetServiceInterface]
public partial interface IRemoteBenchRunner : INetService
{
    NetTask<NetResult> Start(int total, int concurrent, string? remoteNode, string? remotePrivateKey);
}
