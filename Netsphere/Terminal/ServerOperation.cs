// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

internal class ServerOperation : NetOperation
{
    internal ServerOperation(NetTerminal netTerminal)
        : base(netTerminal)
    {
        this.receiverInterface = NetInterface<object, byte[]>.CreateReceive(this);
    }

    public unsafe void SendClose()
    {
        if (this.senderInterface == null)
        {
            throw new InvalidOperationException();
        }

        this.NetTerminal.CreateHeader(out var header, this.senderInterface.StandbyGene);
        header.Id = PacketId.Close;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.NetTerminal.Endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
    }

    public async Task<NetInterfaceReceivedData> ReceiveAsync(int millisecondsToWait)
    {
        if (this.receiverInterface == null)
        {
            throw new InvalidOperationException();
        }

        var received = await this.receiverInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
        if (received.Result != NetInterfaceResult.Success)
        {// Error
            return received;
        }

        // Success
        if (received.PacketId != PacketId.Reserve)
        {
            return received;
        }

        // PacketId.Reserve
        if (!TinyhandSerializer.TryDeserialize<PacketReserve>(received.Received, out var reserve))
        {
            return new(NetInterfaceResult.DeserializationError);
        }

        this.receiverInterface2 = NetInterface<object, byte[]>.CreateReceive(this);
        this.receiverInterface2.SetReserve(reserve);

        this.receiverInterface.SetSend(new PacketReserveResponse());

        received = await this.receiverInterface2.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
        return received;
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait)
       where TSend : IPacket
    {
        if (this.senderInterface == null)
        {
            throw new InvalidOperationException();
        }

        this.senderInterface.SetSend(value);
        try
        {
            return await this.senderInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            this.senderInterface.Dispose();
        }
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait)
    {
        if (this.senderInterface == null)
        {
            throw new InvalidOperationException();
        }

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

    public async Task<NetInterfaceResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.receiverInterface?.Dispose();
                this.receiverInterface2?.Dispose();
                this.senderInterface?.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;

            base.Dispose(disposing);
        }
    }

    private bool disposed = false; // To detect redundant calls.
    private NetInterface<object, byte[]>? receiverInterface;
    private NetInterface<object, byte[]>? receiverInterface2;
    private NetInterface<object, byte[]>? senderInterface;
}
