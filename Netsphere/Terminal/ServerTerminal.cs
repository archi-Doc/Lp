// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class ServerTerminal : NetTerminal
{
    public const ushort DefaultReceiverNumber = 4;
    public const ushort MaxReceiverNumber = 16;

    internal ServerTerminal(Terminal terminal, NodeInformation nodeInformation, ulong gene)
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

        var operation = this.GetSender();
        operation.SendClose();
        this.NextSender();
    }

    public async Task<NetInterfaceReceivedData> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        var operation = this.GetReceiver();
        var received = await operation.ReceiveAsync(millisecondsToWait);
        if (received.Result == NetInterfaceResult.Timeout)
        {// Timeout
            return received;
        }
        else if (received.Result != NetInterfaceResult.Success)
        {// Error
            this.NextReceiver();
            return received;
        }

        this.ReceiverToSender();
        return received;
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted && !this.IsEncrypted)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        var operation = this.GetSender();
        var result = await operation.SendPacketAsync(value, millisecondsToWait);
        if (result == NetInterfaceResult.Timeout)
        {// Timeout
            return result;
        }
        else if (result != NetInterfaceResult.Success)
        {// Error
            this.NextSender();
            return result;
        }

        // Success
        this.NextSender();
        return result;
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
            var operation = new ServerOperation(this);
            this.receiverQueue.Enqueue(operation);
        }
    }

    public void ClearSender()
    {
        while (this.senderQueue.TryDequeue(out var netInterface))
        {
            netInterface.Dispose();
        }
    }

    internal ServerOperation CreateOperation() => new ServerOperation(this);

    private ServerOperation GetReceiver()
    {
        this.EnsureReceiver();
        return this.receiverQueue.Peek();
    }

    private void NextReceiver()
    {
        this.EnsureReceiver();
        this.receiverQueue.Dequeue().Dispose();
    }

    private ServerOperation GetSender() => this.senderQueue.Peek();

    private void NextSender() => this.senderQueue.Dequeue().Dispose();

    private void ReceiverToSender()
    {
        if (this.receiverQueue.TryDequeue(out var netInterface))
        {
            this.senderQueue.Enqueue(netInterface);
        }
    }

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private Queue<ServerOperation> senderQueue = new();
    private Queue<ServerOperation> receiverQueue = new();
}
