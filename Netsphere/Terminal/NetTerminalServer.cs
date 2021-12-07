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
    public const ushort DefaultReceiverNumber = 4;
    public const ushort MaxReceiverNumber = 16;

    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }

    public void SetReceiverNumber(ushort receiverNumber = DefaultReceiverNumber)
    {
        if (receiverNumber > MaxReceiverNumber)
        {
            receiverNumber = MaxReceiverNumber;
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

        if (received.PacketId == PacketId.Reserve)
        {
            if (!TinyhandSerializer.TryDeserialize<PacketReserve>(received.Value, out var reservePacket))
            {
                this.NextReceiver();
                return (NetInterfaceResult.DeserializationError, null);
            }

        }

        this.ReceiverToSender();
        var packet = new NetTerminalServerPacket(received.PacketId, received.Value);
        return (received.Result, packet);
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
            netInterface.Dispose();
        }
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {// checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return NetInterfaceResult.SerializationError;
        }

        Task<NetInterfaceResult> task;
        if (value is IPacket packet)
        {
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.Id, (ulong)packet.Id, owner, millisecondsToWait);
        }
        else
        {
            var id = BlockService.GetId<TSend>();
            task = this.SendDataAsync(true, PacketId.Data, id, owner, millisecondsToWait);
        }

        owner.Return();
        return await task.ConfigureAwait(false);
    }

    public async Task<NetInterfaceResult> SendDataAsync(ulong id, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, id, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

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

    private async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong id, ByteArrayPool.MemoryOwner owner, int millisecondsToWait = DefaultMillisecondsToWait)
    {// checked
        if (encrypt && !this.IsEncrypted)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }
        else if (owner.Memory.Length > BlockService.MaxBlockSize)
        {// Block size limit exceeded.
            return NetInterfaceResult.BlockSizeLimit;
        }

        NetInterface<object, byte[]>? netInterface;
        if (!this.senderQueue.TryDequeue(out netInterface))
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
            netInterface.SetSend(packetId, id, owner);
        }
        else
        {// Split into multiple packets. Send PacketReserve.
            var reserve = new PacketReserve(owner.Memory.Length);
            netInterface.SetSend(reserve);
            await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }

        // netInterface.SetSend(reserve);

        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
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
