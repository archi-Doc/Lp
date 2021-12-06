// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public struct NetTerminalServerPacket
{
    public PacketId PacketId;
    public uint Id;
    public ByteArrayPool.MemoryOwner Owner;
}

public class NetTerminalServer : NetTerminal
{
    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }

    public async Task<(NetInterfaceResult Result, NetTerminalServerPacket? Packet)> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        NetInterface<object, byte[]>? netInterface;
        if (!this.receiveQueue.TryPeek(out netInterface))
        {
            return (NetInterfaceResult.Timeout, null);
        }

        try
        {
            var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
            if (received.Result != NetInterfaceResult.Success)
            {
                return (received.Result, null);
            }

            return (received.Result, new());
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    internal void EnsureReceiveQueue(int numberOfReceive = 1)
    {
        while (this.receiveQueue.Count < numberOfReceive)
        {
            var netInterface = NetInterface<object, byte[]>.CreateReceive(this);
            this.receiveQueue.Enqueue(netInterface);
        }
    }

    private Queue<NetInterface<object, byte[]>> receiveQueue = new();
}
