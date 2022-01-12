// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace Netsphere;

public class MyStatus
{
    public enum ConnectionType
    {
        Unknown,
        Global,
        NAT,
        Symmetric,
    }

    public MyStatus()
    {
    }

    public ulong IncrementServerCount() => Interlocked.Increment(ref this.serverCount);

    public ulong ServerCount => Volatile.Read(ref this.serverCount);

    public ConnectionType Type { get; private set; }

    public double EstimatedMBPS { get; private set; }

    private ulong serverCount;
}
