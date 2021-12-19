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

    public override async Task<NetResult> EncryptConnectionAsync(int millisecondsToWait)
    {// Checked
        if (this.NetTerminal.IsEncrypted)
        {// Encrypted
            return NetResult.Success;
        }
        else if (this.NetTerminal.NodeInformation == null && !this.Terminal.NetBase.AllowUnsafeConnection)
        {// Unmanaged
            return NetResult.NoNodeInformation;
        }

        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation(this.Terminal.IsAlternative));
        var response = await this.SendPacketAndReceiveAsync<PacketEncrypt, PacketEncryptResponse>(p, millisecondsToWait).ConfigureAwait(false);
        if (response.Result != NetResult.Success)
        {
            return response.Result;
        }

        return this.NetTerminal.CreateEmbryo(p.Salt);
    }

    public async Task<NetResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait)
        where TSend : IPacket
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                return NetResult.NoEncryptedConnection;
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

    public async Task<(NetResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait)
        where TSend : IPacket
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminal.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetResult.Success)
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
            var response = await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);

            if (response.PacketId == PacketId.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserve>(response.Received.Memory, out var reserve);
                response.Return();
                if (reserve == null)
                {
                    return new(NetResult.DeserializationError, default);
                }

                var netInterface2 = NetInterface<PacketReserveResponse, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult, default);
                }

                try
                {
                    response = await netInterface2.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
                }
                finally
                {
                    netInterface2.Dispose();
                }
            }

            if (response.Result != NetResult.Success)
            {
                response.Return();
                return (response.Result, default);
            }

            TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory, out var r);
            response.Return();
            if (r == null)
            {
                return (NetResult.DeserializationError, default);
            }

            return (NetResult.Success, r);
        }
        finally
        {
            netInterface.Dispose();
        }
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

    public async Task<NetResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait)
    {// Checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        Task<NetReceivedData> task;
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
        if (response.Result != NetResult.Success)
        {
            return (response.Result, default);
        }

        if (!TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory, out var received))
        {
            return (NetResult.DeserializationError, default);
        }

        return (response.Result, received);
    }

    public async Task<(NetResult Result, ByteArrayPool.MemoryOwner Value)> SendAndReceiveDataAsync(ulong dataId, byte[] data, int millisecondsToWait)
    {// Checked
        var response = await this.SendAndReceiveDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
        return (response.Result, response.Received);
    }

    internal async Task<NetResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {// Checked
        if (!this.NetTerminal.IsEncrypted && encrypt)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetResult.Success)
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
            if (received.Result != NetResult.Success)
            {
                return received.Result;
            }
        }
        else
        {// Block size limit exceeded.
            return NetResult.BlockSizeLimit;
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

    internal async Task<NetReceivedData> SendAndReceiveDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, int millisecondsToWait)
    {// Checked
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
            if (result != NetResult.Success)
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
            if (received.Result != NetResult.Success)
            {
                return new(received.Result);
            }
        }
        else
        {// Block size limit exceeded.
            return new(NetResult.BlockSizeLimit);
        }

        var netInterface = NetInterface<byte[], byte[]>.CreateData(this, packetId, dataId, owner, true, out var interfaceResult);
        if (netInterface == null)
        {
            return new(interfaceResult);
        }

        try
        {
            var response = await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);

            if (response.PacketId == PacketId.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserve>(response.Received.Memory, out var reserve);
                if (reserve == null)
                {
                    return new(NetResult.DeserializationError);
                }

                var netInterface2 = NetInterface<PacketReserveResponse, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult);
                }

                try
                {
                    response = await netInterface2.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
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
