// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class MyStatus
{
    public MyStatus()
    {
    }

    public ulong IncrementServerCount() => Interlocked.Increment(ref this.serverCount);

    public ulong ServerCount => Volatile.Read(ref this.serverCount);

    public double EstimatedMBPS { get; private set; }

    private ulong serverCount;
}
