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
        this.EnsureInterface();
    }

    public async Task<(NetInterfaceResult Result, NetTerminalServerPacket? Packet)> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        this.EnsureInterface();
        var netInterface = this.GetInterface();

        var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
        if (received.Result == NetInterfaceResult.Timeout)
        {// Timeout
            return (received.Result, null);
        }

        if (received.Result != NetInterfaceResult.Success || received.Value == null)
        {// Error
            this.NextInterface();
            return (received.Result, null);
        }

        var packet = new NetTerminalServerPacket(received.PacketId, received.Value);
        return (received.Result, packet);
    }

    public void EnsureInterface()
    {
        while (this.interfaceQueue.Count < this.ReceiverNumber)
        {
            var netInterface = NetInterface<object, byte[]>.CreateReceive(this);
            this.interfaceQueue.Enqueue(netInterface);
        }
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted && !this.IsEncrypted)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        var netInterface = this.GetInterface();
        netInterface.SetSend(value);
        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            this.NextInterface();
        }
    }

    private NetInterface<object, byte[]> GetInterface()
    {
        this.EnsureInterface();
        return this.interfaceQueue.Peek();
    }

    private void NextInterface()
    {
        this.EnsureInterface();
        this.interfaceQueue.Dequeue().Dispose();
    }

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private Queue<NetInterface<object, byte[]>> interfaceQueue = new();
}
