// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

internal class ClientOperation : NetOperation
{
    internal ClientOperation(NetTerminal netTerminal)
        : base(netTerminal)
    {
    }

    public override async Task<NetInterfaceResult> EncryptConnectionAsync(int millisecondsToWait)
    {// Checked
        if (this.NetTerminal.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NetTerminal.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var response = await this.SendPacketAndReceiveAsync<PacketEncrypt, PacketEncryptResponse>(p, millisecondsToWait).ConfigureAwait(false);
        if (response.Result != NetInterfaceResult.Success)
        {
            return response.Result;
        }

        return this.NetTerminal.CreateEmbryo(p.Salt);
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait)
        where TSend : IPacket
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return NetInterfaceResult.NoEncryptedConnection;
            }
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
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return (result, default);
            }
        }

        var netInterface = NetInterface<TSend, TReceive>.CreateValue(this, value, value.PacketId, true, out var interfaceResult);
        if (netInterface == null)
        {
            return (interfaceResult, default);
        }

        try
        {
            var response = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);

            if (response.PacketId == PacketId.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserve>(response.Received.Memory, out var reserve);
                if (reserve == null)
                {
                    return new(NetInterfaceResult.DeserializationError, default);
                }

                var netInterface2 = NetInterface<PacketReserveResponse, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult, default);
                }

                try
                {
                    response = await netInterface2.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
                }
                finally
                {
                    netInterface2.Dispose();
                }
            }

            if (response.Result != NetInterfaceResult.Success)
            {
                response.Received.Return();
                return (response.Result, default);
            }

            TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory, out var r);
            if (r == null)
            {
                return (NetInterfaceResult.DeserializationError, default);
            }

            return (NetInterfaceResult.Success, r);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait)
    {// Checked
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
    {// Checked
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

        if (!TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory, out var received))
        {
            return (NetInterfaceResult.DeserializationError, default);
        }

        return (response.Result, received);
    }

    public async Task<(NetInterfaceResult Result, ByteArrayPool.MemoryOwner Value)> SendAndReceiveDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
    {// Checked
        var response = await this.SendAndReceiveDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
        return (response.Result, response.Received);
    }

    internal async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {// Checked
        if (!this.NetTerminal.IsEncrypted && encrypt)
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
    {// Checked
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
            var response = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);

            if (response.PacketId == PacketId.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserve>(response.Received.Memory, out var reserve);
                if (reserve == null)
                {
                    return new(NetInterfaceResult.DeserializationError);
                }

                var netInterface2 = NetInterface<PacketReserveResponse, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult);
                }

                try
                {
                    response = await netInterface2.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
                }
                finally
                {
                    netInterface2.Dispose();
                }
            }

            return response;
        }
        finally
        {
            netInterface.Dispose();
        }
    }
}
