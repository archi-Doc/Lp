﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

internal static class PacketPool
{
    public const int MaxPacketSize = 2048;

    static PacketPool()
    {
        packetPool = new FixedArrayPool(MaxPacketSize);
    }

    public static FixedArrayPool.Owner Rent() => packetPool.Rent();

    private static FixedArrayPool packetPool;
}
