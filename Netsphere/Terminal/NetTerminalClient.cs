// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetTerminalClient : NetTerminal
{
    internal NetTerminalClient(Terminal terminal, NodeAddress nodeAddress)
        : base(terminal, nodeAddress)
    {// NodeAddress: Unmanaged
    }

    internal NetTerminalClient(Terminal terminal, NodeInformation nodeInformation)
        : base(terminal, nodeInformation, LP.Random.Crypto.NextULong())
    {// NodeInformation: Managed
    }

    public override async Task<NetInterfaceResult> EncryptConnectionAsync()
    {
        if (this.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var response = await this.SendPacketAndReceiveAsync<PacketEncrypt, PacketEncryptResponse>(p).ConfigureAwait(false);
        if (response.Result != NetInterfaceResult.Success)
        {
            return response.Result;
        }

        return this.CreateEmbryo(p.Salt);
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {// checked
        if (!value.AllowUnencrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return NetInterfaceResult.NoEncryptedConnection;
            }
        }

        var netInterface = this.CreateSendValue(value);
        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {// checked
        if (!value.AllowUnencrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return (result, default);
            }
        }

        var netInterface = this.CreateSendAndReceiveValue<TSend, TReceive>(value);
        try
        {
            return await netInterface.ReceiveAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return NetInterfaceResult.SerializationError;
        }

        Task<NetInterfaceResult> task;
        if (value is IPacket packet)
        {
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.Id, 0, owner, millisecondsToWait);
        }
        else
        {
            ulong id;
            if (value is IBlock block)
            {
                id = block.Id;
            }

            task = this.SendDataAsync(true, PacketId.Data, (ulong)id, owner, millisecondsToWait);
        }

        owner.Return();
        return task.Result;
    }

    public async Task<NetInterfaceResult> SendDataAsync(ulong id, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, id, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return (NetInterfaceResult.SerializationError, default);
        }

        Task<(NetInterfaceResult Result, byte[]? Value)> task;
        if (value is IPacket packet)
        {
            task = this.SendAndReceiveDataAsync(!packet.AllowUnencrypted, packet.Id, 0, owner, millisecondsToWait);
        }
        else
        {
            task = this.SendAndReceiveDataAsync(true, PacketId.Data, 0, owner, millisecondsToWait);
        }

        owner.Return();

        var response = task.Result;
        if (response.Result != NetInterfaceResult.Success)
        {
            return (response.Result, default);
        }

        TinyhandSerializer.TryDeserialize<TReceive>(response.Value, out var received);
        if (received == null)
        {
            return (NetInterfaceResult.DeserializationError, default);
        }

        return (NetInterfaceResult.Success, received);
    }

    public async Task<(NetInterfaceResult Result, byte[]? Value)> SendAndReceiveDataAsync(uint id, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendAndReceiveDataAsync(true, PacketId.Data, id, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

    private async Task<NetInterfaceResult> SendDataAsync(bool encrypt, PacketId packetId, ulong id, ByteArrayPool.MemoryOwner owner, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return default;
            }
        }

        var netInterface = this.CreateSendData(packetId, id, owner);
        try
        {
            return await netInterface.WaitForSendCompletionAsync(millisecondsToWait).ConfigureAwait(false);
        }
        finally
        {
            netInterface.Dispose();
        }
    }

    private async Task<(NetInterfaceResult Result, byte[]? Value)> SendAndReceiveDataAsync(bool encrypt, PacketId packetId, ulong id, ByteArrayPool.MemoryOwner owner, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return (result, default);
            }
        }

        if (owner.Memory.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
        }
        else if (owner.Memory.Length <= BlockService.MaxBlockSize)
        {// Split into multiple packets.
            var reserve = new PacketReserve(owner.Memory.Length);
            var result = await this.SendPacketAsync(reserve, millisecondsToWait).ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return (result, default);
            }
        }
        else
        {// Block size limit exceeded.
            return (NetInterfaceResult.BlockSizeLimit, default);
        }

        var netInterface = this.CreateSendAndReceiveData(packetId, id, owner);
        try
        {
            var r = await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
            return (r.Result, r.Value);
        }
        finally
        {
            netInterface.Dispose();
        }
    }
}
