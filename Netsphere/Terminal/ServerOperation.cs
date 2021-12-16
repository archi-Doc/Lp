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
        this.NetTerminal.CreateHeader(out var header, this.GetGene());
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
        {// Timeout/Error
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
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        var netInterface = this.receiverInterface2 ?? this.receiverInterface;
        if (netInterface == null)
        {
            return NetInterfaceResult.NoSender;
        }

        netInterface.SetSend(value);
        return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait)
    {
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

    internal async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {
        if (!this.NetTerminal.IsEncrypted && encrypt)
        {
            return NetInterfaceResult.NoEncryptedConnection;
        }

        var netInterface = this.receiverInterface2 ?? this.receiverInterface;
        if (netInterface == null)
        {
            return NetInterfaceResult.NoSender;
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            netInterface.SetSend(packetId, dataId, owner);
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        else if (owner.Memory.Length > BlockService.MaxBlockSize)
        {// Block size limit exceeded.
            return NetInterfaceResult.BlockSizeLimit;
        }

        // Split into multiple packets. Send PacketReserve.
        var reserve = new PacketReserve(owner.Memory.Length);
        netInterface.SetSend(reserve);
        /*var result = await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        if (result != NetInterfaceResult.Success)
        {
            return result;
        }*/

        netInterface = NetInterface<object, byte[]>.CreateReceive(this);
        try
        {
            var received = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
            if (received.Result != NetInterfaceResult.Success)
            {// Timeout/Error
                return received.Result;
            }
            else if (received.PacketId != PacketId.ReserveResponse)
            {
                return NetInterfaceResult.ReserveError;
            }

            netInterface.SetSend(packetId, dataId, owner);
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

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
            }

            // free native resources here if there are any.
            this.disposed = true;

            base.Dispose(disposing);
        }
    }

    private bool disposed = false; // To detect redundant calls.
    private NetInterface<object, byte[]>? receiverInterface;
    private NetInterface<object, byte[]>? receiverInterface2;
}
