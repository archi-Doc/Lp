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
    {
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
    {
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
    {// checked
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
}
