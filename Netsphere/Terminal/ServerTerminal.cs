// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

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

        if (this.senderQueue.TryDequeue(out var operation))
        {
            operation.SendClose();
        }
    }

    public async Task<NetInterfaceReceivedData> ReceiveAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        this.EnsureReceiver();
        if (!this.receiverQueue.TryDequeue(out var operation))
        {
            return new(NetInterfaceResult.NoReceiver);
        }

        var received = await operation.ReceiveAsync(millisecondsToWait);
        if (received.Result != NetInterfaceResult.Success)
        {// Timeout or error
            return received;
        }

        this.senderQueue.Enqueue(operation);
        return received;
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        if (!this.senderQueue.TryDequeue(out var operation))
        {
            return NetInterfaceResult.NoSender;
        }

        var result = await operation.SendPacketAsync(value, millisecondsToWait);
        operation.Dispose();
        return result;
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (!this.senderQueue.TryDequeue(out var operation))
        {
            return NetInterfaceResult.NoSender;
        }

        var result = await operation.SendAsync(value, millisecondsToWait);
        operation.Dispose();
        return result;
    }

    public async Task<NetInterfaceResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (!this.senderQueue.TryDequeue(out var operation))
        {
            return NetInterfaceResult.NoSender;
        }

        var result = await operation.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
        operation.Dispose();
        return result;
    }

    public void EnsureReceiver()
    {
        while (this.receiverQueue.Count < this.ReceiverNumber)
        {
            this.receiverQueue.Enqueue(new ServerOperation(this));
        }
    }

    public void ClearSender()
    {
        while (this.senderQueue.TryDequeue(out var operation))
        {
            operation.Dispose();
        }
    }

    internal ServerOperation CreateOperation() => new ServerOperation(this);

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private ConcurrentQueue<ServerOperation> senderQueue = new();
    private ConcurrentQueue<ServerOperation> receiverQueue = new();
}
