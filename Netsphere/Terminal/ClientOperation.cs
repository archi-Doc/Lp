// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere;

internal class ClientOperation : NetOperation
{
    internal ClientOperation(NetTerminalObsolete netTerminal)
        : base(netTerminal)
    {
    }

    public override async Task<NetResult> EncryptConnectionAsync()
    {// Checked
        if (this.NetTerminalObsolete.IsEncrypted)
        {// Encrypted
            return NetResult.Success;
        }
        else if (this.NetTerminalObsolete.Node == null && !this.Terminal.NetBase.AllowUnsafeConnection)
        {// Unmanaged
            return NetResult.NoNodeInformation;
        }

        await this.NetTerminalObsolete.ConnectionSemaphore.EnterAsync().ConfigureAwait(false); // Avoid simultaneous invocation.
        try
        {
            if (this.NetTerminalObsolete.IsEncrypted)
            {// Encrypted
                return NetResult.Success;
            }

            if (this.NetTerminalObsolete.Node == null)
            {// Get NodeInformation (Unsafe).
                var r = await this.SendPacketAndReceiveAsync<PacketGetNodeInformationObsolete, PacketGetNodeInformationResponseObsolete>(new()).ConfigureAwait(false);
                if (r.Result != NetResult.Success)
                {
                    return r.Result;
                }

                this.NetTerminalObsolete.MergeNode(r.Value!.Node);
            }

            // Encrypt
            var p = new PacketEncryptObsolete(default!); // tempcode
            var response = await this.SendPacketAndReceiveAsync<PacketEncryptObsolete, PacketEncryptResponseObsolete>(p).ConfigureAwait(false);
            if (response.Result != NetResult.Success)
            {
                return response.Result;
            }

            this.NetTerminalObsolete.SetSalt(p.SaltA, response.Value!.SaltA2);
            return this.NetTerminalObsolete.CreateEmbryo(p.Salt, response.Value!.Salt2);
        }
        finally
        {
            this.NetTerminalObsolete.ConnectionSemaphore.Exit();
        }
    }

    public async Task<NetResult> SendPacketAsync<TSend>(TSend value)
        where TSend : IPacketObsolete
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminalObsolete.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
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
            return await netInterface.WaitForSendCompletionAsync().ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    public async Task<(NetResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value)
        where TSend : IPacketObsolete
    {// Checked
        if (!value.AllowUnencrypted && !this.NetTerminalObsolete.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
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
            var response = await netInterface.ReceiveAsync().ConfigureAwait(false);

            if (response.PacketId == PacketIdObsolete.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserveObsolete>(response.Received.Memory.Span, out var reserve);
                response.Return();
                if (reserve == null)
                {
                    return new(NetResult.DeserializationError, default);
                }

                var netInterface2 = NetInterface<PacketReserveResponseObsolete, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult, default);
                }

                try
                {
                    response = await netInterface2.ReceiveAsync().ConfigureAwait(false);
                }
                finally
                {
                    netInterface2.Dispose();
                }
            }

            if (response.Result != NetResult.Success)
            {
                return (response.Result, default);
            }

            TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory.Span, out var r);
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

    public async Task<NetResult> SendAsync<TSend>(TSend value)
    {// Checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return NetResult.SerializationError;
        }

        Task<NetResult> task;
        if (value is IPacketObsolete packet)
        {
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.PacketId, (ulong)packet.PacketId, owner);
        }
        else
        {
            var dataId = BlockService.GetId<TSend>();
            task = this.SendDataAsync(true, PacketIdObsolete.Data, dataId, owner);
        }

        owner.Return();
        return await task.ConfigureAwait(false);
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value)
    {// Checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        Task<NetReceivedData> task;
        ulong dataId;
        if (value is IPacketObsolete packet)
        {
            dataId = (ulong)packet.PacketId | ((ulong)BlockService.GetId<TReceive>() << 32);
            task = this.SendAndReceiveDataAsync(!packet.AllowUnencrypted, packet.PacketId, dataId, owner);
        }
        else
        {
            dataId = BlockService.GetId<TSend, TReceive>();
            task = this.SendAndReceiveDataAsync(true, PacketIdObsolete.Data, dataId, owner);
        }

        owner.Return();

        var response = await task.ConfigureAwait(false);
        if (response.Result != NetResult.Success)
        {
            return (response.Result, default);
        }

        if (!TinyhandSerializer.TryDeserialize<TReceive>(response.Received.Memory.Span, out var received))
        {
            response.Return();
            return (NetResult.DeserializationError, default);
        }

        response.Return();
        return (response.Result, received);
    }

    internal async Task<NetResult> SendDataAsync(bool encrypt, PacketIdObsolete packetId, ulong dataId, ByteArrayPool.MemoryOwner owner)
    {// Checked
        if (!this.NetTerminalObsolete.IsEncrypted && encrypt)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
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
            var reserve = new PacketReserveObsolete(owner.Memory.Length);
            var received = await this.SendPacketAndReceiveAsync<PacketReserveObsolete, PacketReserveResponseObsolete>(reserve).ConfigureAwait(false);
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
            return await netInterface.WaitForSendCompletionAsync().ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    internal async Task<NetReceivedData> SendAndReceiveDataAsync(bool encrypt, PacketIdObsolete packetId, ulong dataId, ByteArrayPool.MemoryOwner owner)
    {// Checked
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
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
            var reserve = new PacketReserveObsolete(owner.Memory.Length);
            var received = await this.SendPacketAndReceiveAsync<PacketReserveObsolete, PacketReserveResponseObsolete>(reserve).ConfigureAwait(false);
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
            var response = await netInterface.ReceiveAsync().ConfigureAwait(false);

            if (response.PacketId == PacketIdObsolete.Reserve)
            {
                // PacketId.Reserve
                TinyhandSerializer.TryDeserialize<PacketReserveObsolete>(response.Received.Memory.Span, out var reserve);
                response.Return();
                if (reserve == null)
                {
                    return new(NetResult.DeserializationError);
                }

                var netInterface2 = NetInterface<PacketReserveResponseObsolete, byte[]>.CreateReserve(this, reserve);
                if (netInterface2 == null)
                {
                    return new(interfaceResult);
                }

                try
                {
                    response = await netInterface2.ReceiveAsync().ConfigureAwait(false);
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
