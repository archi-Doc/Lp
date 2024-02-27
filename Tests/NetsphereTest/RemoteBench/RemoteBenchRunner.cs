// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;

namespace LP.NetServices;

[NetServiceInterface]
public partial interface RemoteBenchRunner : INetService
{
    NetTask<NetResult> Start(int total, int concurrent);
}

public interface INetServiceHandler
{
    void OnConnected();

    void OnDisconnected();
}
