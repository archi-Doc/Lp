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

        var netInterface = NetInterface<TSend, object>.CreateValue(this, value, value.PacketId, false, out var interfaceResult);
        if (netInterface == null)
        {
            return interfaceResult;
        }

        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait)
        where TSend : IPacket
    {
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            return (NetInterfaceResult.NoEncryptedConnection, default);
        }

        var netInterface = NetInterface<TSend, TReceive>.CreateValue(this, value, value.PacketId, true, out var interfaceResult);
        if (netInterface == null)
        {
            return (interfaceResult, default);
        }

        try
        {
            var response = await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
            if (response.Result != NetInterfaceResult.Success)
            {
                return response;
            }

            if (response.Value is PacketReserve reserve)
            {

            }

            return await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
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

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait)
    {// checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return (NetInterfaceResult.SerializationError, default);
        }

        Task<NetInterfaceReceivedData> task;
        ulong dataId;
        if (value is IPacket packet)
        {
            dataId = (ulong)packet.PacketId | ((ulong)BlockService.GetId<TReceive>() << 32);
            task = this.SendAndReceiveDataAsync(!packet.AllowUnencrypted, packet.PacketId, dataId, owner, millisecondsToWait);
        }
        else
        {
            dataId = BlockService.GetId<TSend, TReceive>();
            task = this.SendAndReceiveDataAsync(true, PacketId.Data, dataId, owner, millisecondsToWait);
        }

        owner.Return();

        var response = await task.ConfigureAwait(false);
        if (response.Result != NetInterfaceResult.Success)
        {
            return (response.Result, default);
        }

        // var dataMemory = PacketService.GetData(response.Received);
        TinyhandSerializer.TryDeserialize<TReceive>(response.Received, out var received);
        if (received == null)
        {
            return (NetInterfaceResult.DeserializationError, default);
        }

        return (NetInterfaceResult.Success, received);
    }

    public async Task<(NetInterfaceResult Result, ReadOnlyMemory<byte> Value)> SendAndReceiveDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
    {
        var response = await this.SendAndReceiveDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
        return (response.Result, response.Received);
    }

    internal async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return result;
            }
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
        }
        else if (owner.Memory.Length <= BlockService.MaxBlockSize)
        {// Split into multiple packets. Send PacketReserve.
            var reserve = new PacketReserve(owner.Memory.Length);
            var received = await this.SendPacketAndReceiveAsync<PacketReserve, PacketReserveResponse>(reserve, millisecondsToWait).ConfigureAwait(false);
            if (received.Result != NetInterfaceResult.Success)
            {
                return received.Result;
            }
        }
        else
        {// Block size limit exceeded.
            return NetInterfaceResult.BlockSizeLimit;
        }

        var netInterface = NetInterface<byte[], object>.CreateData(this, packetId, dataId, owner, false, out var interfaceResult);
        if (netInterface == null)
        {
            return interfaceResult;
        }

        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    internal async Task<NetInterfaceReceivedData> SendAndReceiveDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {// checked
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return new(result);
            }
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
        }
        else if (owner.Memory.Length <= BlockService.MaxBlockSize)
        {// Split into multiple packets. Send PacketReserve.
            var reserve = new PacketReserve(owner.Memory.Length);
            var received = await this.SendPacketAndReceiveAsync<PacketReserve, PacketReserveResponse>(reserve, millisecondsToWait).ConfigureAwait(false);
            if (received.Result != NetInterfaceResult.Success)
            {
                return new(received.Result);
            }
        }
        else
        {// Block size limit exceeded.
            return new(NetInterfaceResult.BlockSizeLimit);
        }

        var netInterface = NetInterface<byte[], byte[]>.CreateData(this, packetId, dataId, owner, true, out var interfaceResult);
        if (netInterface == null)
        {
            return new(interfaceResult);
        }

        try
        {
            var r = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
            return r;
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
