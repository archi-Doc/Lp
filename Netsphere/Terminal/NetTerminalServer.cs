// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetTerminalServerPacket
{
    public unsafe NetTerminalServerPacket(PacketId packetId, byte[] data)
    {
        this.PacketId = packetId;
        this.Data = data;

        if (this.PacketId == PacketId.Data && this.Data.Length >= PacketService.DataHeaderSize)
        {// PacketData
            var span = this.Data.Span;
            DataHeader dataHeader = default;
            fixed (byte* pb = span)
            {
                dataHeader = *(DataHeader*)pb;
            }

            span = span.Slice(PacketService.DataHeaderSize);
            if (Arc.Crypto.FarmHash.Hash64(span) != dataHeader.Checksum)
            {
                return;
            }

            this.PacketId = dataHeader.PacketId;
            this.Id = dataHeader.Id;
            this.Data = this.Data.Slice(PacketService.DataHeaderSize);
        }
    }

    public PacketId PacketId { get; }

    public uint Id { get; }

    public Memory<byte> Data { get; }
}

public class NetTerminalServer : NetTerminal
{
    public const ushort DefaultReceiverNumber = 1;

    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }

    public void SetReceiverNumber(ushort receiverNumber = DefaultReceiverNumber)
    {
        if (receiverNumber > DefaultReceiverNumber)
        {
            receiverNumber = DefaultReceiverNumber;
        }
        else if (receiverNumber == 0)
        {
            receiverNumber = 1;
        }

        this.ReceiverNumber = receiverNumber;
        this.EnsureInterfaceQueue();
    }

    public async Task<(NetInterfaceResult Result, NetTerminalServerPacket? Packet)> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        NetInterface<object, byte[]>? netInterface;
        if (!this.interfaceQueue.TryPeek(out netInterface))
        {
            return (NetInterfaceResult.Timeout, null);
        }

        var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
        if (received.Result == NetInterfaceResult.Timeout)
        {// Timeout
            return (received.Result, null);
        }

        this.interfaceQueue.TryDequeue(out _);
        this.EnsureInterfaceQueue();
        if (received.Result != NetInterfaceResult.Success || received.Value == null)
        {// Error
            netInterface.Dispose();
            return (received.Result, null);
        }

        var packet = new NetTerminalServerPacket(received.PacketId, received.Value);
        this.senderQueue.Enqueue(netInterface);
        return (received.Result, packet);
    }

    public void EnsureInterfaceQueue()
    {
        while (this.interfaceQueue.Count < this.ReceiverNumber)
        {
            var netInterface = NetInterface<object, byte[]>.CreateReceive(this);
            this.interfaceQueue.Enqueue(netInterface);
        }
    }

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private NetInterface<object, byte[]> currentInterface = default!;
    private Queue<NetInterface<object, byte[]>> interfaceQueue = new();
}
