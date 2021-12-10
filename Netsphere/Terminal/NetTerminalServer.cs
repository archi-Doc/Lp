// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/*public class NetTerminalServerPacket
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

            this.PacketId = dataHeader.PacketId;
            this.DataId = dataHeader.DataId;
            this.Data = this.Data.Slice(PacketService.DataHeaderSize);
        }
    }

    public PacketId PacketId { get; }

    public ulong DataId { get; }

    public Memory<byte> Data { get; }
}*/

public class NetTerminalServer : NetTerminal
{
    public const ushort DefaultReceiverNumber = 4;
    public const ushort MaxReceiverNumber = 16;

    internal NetTerminalServer(Terminal terminal, NodeInformation nodeInformation, ulong gene)
        : base(terminal, nodeInformation, gene)
    {// NodeInformation: Managed
    }

    public unsafe override void SendClose()
    {
        if (this.IsClosed)
        {
            return;
        }

        this.IsClosed = true;
        if (!this.IsEncrypted)
        {// Not encrypted (connected)
            return;
        }

        NetInterface<object, byte[]>? netInterface;
        if (!this.senderQueue.TryDequeue(out netInterface))
        {
            return;
        }

        this.CreateHeader(out var header, netInterface.StandbyGene);
        header.Id = PacketId.Close;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.Endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
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

    public async Task<NetInterfaceReceivedData> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        PacketReserve? reserve = null;
        NetInterface<object, byte[]>? reserveInterface = null;

        try
        {
            var netInterface = this.GetReceiver();
ReceiveAsyncStart:

            var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
            if (received.Result == NetInterfaceResult.Timeout)
            {// Timeout
                return received;
            }

            if (received.Result != NetInterfaceResult.Success)
            {// Error
                this.NextReceiver();
                return received;
            }

            if (received.PacketId == PacketId.Reserve)
            {
                if (reserve != null)
                {
                    this.NextReceiver();
                    return new (NetInterfaceResult.ReserveError);
                }
                else if (!TinyhandSerializer.TryDeserialize<PacketReserve>(received.Received, out reserve))
                {
                    this.NextReceiver();
                    return new (NetInterfaceResult.DeserializationError);
                }

                reserveInterface = netInterface;
                this.ReceiverToSender();
                netInterface = this.GetReceiver();
                netInterface.SetReserve(reserve);

                reserveInterface.SetSend(new PacketReserveResponse());
                goto ReceiveAsyncStart;
            }

            this.ReceiverToSender();
            return received;
        }
        finally
        {
            reserveInterface?.Dispose();
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
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.PacketId, (ulong)packet.PacketId, owner, millisecondsToWait);
        }
        else
        {
            var dataId = BlockService.GetId<TSend>();
            task = this.SendDataAsync(true, PacketId.Data, dataId, owner, millisecondsToWait);
        }

        owner.Return();
        return await task.ConfigureAwait(false);
    }

    public async Task<NetInterfaceResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

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

    private async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait = DefaultMillisecondsToWait)
    {// checked
        NetInterface<object, byte[]>? reserveInterface = null;

        try
        {
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
                netInterface.SetSend(packetId, dataId, owner);
            }
            else
            {// Split into multiple packets. Send PacketReserve.
                reserveInterface = netInterface;
                var reserve = new PacketReserve(owner.Memory.Length);
                reserveInterface.SetSend(reserve);

                netInterface = this.GetReceiver();
                var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
                if (received.Result != NetInterfaceResult.Success || received.PacketId != PacketId.ReserveResponse)
                {
                    return NetInterfaceResult.ReserveError;
                }

                // netInterface.SetSend(packetId, id, owner);
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
        finally
        {
            reserveInterface?.Dispose();
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
