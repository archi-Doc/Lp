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
    {// Checked
        var netInterface = this.receiverInterface2 ?? this.receiverInterface;
        if (netInterface == null)
        {
            return;
        }

        this.NetTerminal.CreateHeader(out var header, netInterface.StandbyGene);
        header.Id = PacketId.Close;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.NetTerminal.Endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
    }

    public async Task<NetReceivedData> ReceiveAsync(int millisecondsToWait)
    {// Checked
        if (this.receiverInterface == null)
        {
            throw new InvalidOperationException();
        }

        var received = await this.receiverInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
        if (received.Result != NetResult.Success)
        {// Timeout/Error
            return received;
        }

        // Success
        if (received.PacketId != PacketId.Reserve)
        {
            return received;
        }

        // PacketId.Reserve
        if (!TinyhandSerializer.TryDeserialize<PacketReserve>(received.Received.Memory, out var reserve))
        {
            received.Return();
            return new(NetResult.DeserializationError);
        }

        received.Return();
        this.receiverInterface2 = NetInterface<object, byte[]>.CreateReceive(this);
        this.receiverInterface2.SetReserve(this, reserve);

        this.receiverInterface.SetSend(new PacketReserveResponse());

        received = await this.receiverInterface2.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
        return received;
    }

    public async Task<NetResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait)
       where TSend : IPacket
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            return NetResult.NoEncryptedConnection;
        }

        var netInterface = this.receiverInterface2 ?? this.receiverInterface;
        if (netInterface == null)
        {
            return NetResult.NoSender;
        }

        netInterface.SetSend(value);
        return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
    }

    public async Task<NetResult> SendAsync<TSend>(TSend value, int millisecondsToWait)
    {// Checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return NetResult.SerializationError;
        }

        Task<NetResult> task;
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

    /*public async Task<NetResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);*/

    internal async Task<NetResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {// Checked
        if (!this.NetTerminal.IsEncrypted && encrypt)
        {
            return NetResult.NoEncryptedConnection;
        }

        var netInterface = this.receiverInterface2 ?? this.receiverInterface;
        if (netInterface == null)
        {
            return NetResult.NoSender;
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            netInterface.SetSend(this, packetId, dataId, owner);
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        else if (owner.Memory.Length > BlockService.MaxBlockSize)
        {// Block size limit exceeded.
            return NetResult.BlockSizeLimit;
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
            var received = await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
            if (received.Result != NetResult.Success)
            {// Timeout/Error
                return received.Result;
            }
            else if (received.PacketId != PacketId.ReserveResponse)
            {
                return NetResult.ReserveError;
            }

            netInterface.SetSend(this, packetId, dataId, owner);
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
                this.receiverInterface = null;
                this.receiverInterface2 = null;
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
