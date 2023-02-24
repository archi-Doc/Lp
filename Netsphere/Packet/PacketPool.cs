// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

internal static class PacketPool
{
    public const int MaxPacketSize = 2048;

    static PacketPool()
    {
        packetPool = new ByteArrayPool(MaxPacketSize);
    }

    public static ByteArrayPool.Owner Rent() => packetPool.Rent(MaxPacketSize);

    private static ByteArrayPool packetPool;
}
