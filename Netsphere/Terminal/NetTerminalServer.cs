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
            if (!dataHeader.ChecksumEquals(Arc.Crypto.FarmHash.Hash64(span)))
            {
                return;
            }

            this.PacketId = dataHeader.PacketId;
            this.Id = dataHeader.Id;
            this.Data = this.Data.Slice(PacketService.DataHeaderSize);
        }
    }

    public PacketId PacketId { get; }

    public ulong Id { get; }

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
        this.EnsureReceiver();
    }

    public async Task<(NetInterfaceResult Result, NetTerminalServerPacket? Packet)> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        var netInterface = this.GetReceiver();
        var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
        if (received.Result == NetInterfaceResult.Timeout)
        {// Timeout
            return (received.Result, null);
        }

        if (received.Result != NetInterfaceResult.Success || received.Value == null)
        {// Error
            this.NextReceiver();
            return (received.Result, null);
        }

        this.ReceiverToSender();
        var packet = new NetTerminalServerPacket(received.PacketId, received.Value);
        return (received.Result, packet);
    }

    public void EnsureReceiver()
    {
        while (this.receiverQueue.Count < this.ReceiverNumber)
        {
            var netInterface = NetInterface<object, byte[]>.CreateReceive(this);
            this.receiverQueue.Enqueue(netInterface);
        }
    }

    public void ClearSender()
    {
        while (this.senderQueue.TryDequeue(out var netInterface))
        {
            netInterface.Dispose();
        }
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted && !this.IsEncrypted)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        NetInterface<object, byte[]>? netInterface;
        if (!this.senderQueue.TryDequeue(out netInterface))
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        netInterface.SetSend(value);
        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            this.Dispose();
        }
    }

    private NetInterface<object, byte[]> GetReceiver()
    {
        this.EnsureReceiver();
        return this.receiverQueue.Peek();
    }

    private void NextReceiver()
    {
        this.EnsureReceiver();
        this.receiverQueue.Dequeue().Dispose();
    }

    private void ReceiverToSender()
    {
        if (this.receiverQueue.TryDequeue(out var netInterface))
        {
            this.senderQueue.Enqueue(netInterface);
        }
    }

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private Queue<NetInterface<object, byte[]>> senderQueue = new();
    private Queue<NetInterface<object, byte[]>> receiverQueue = new();
}
