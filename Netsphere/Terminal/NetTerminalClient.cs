// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetTerminalClient : NetTerminal
{
    internal NetTerminalClient(Terminal terminal, NodeAddress nodeAddress)
        : base(terminal, nodeAddress)
    {// NodeAddress: Unmanaged
    }

    internal NetTerminalClient(Terminal terminal, NodeInformation nodeInformation)
        : base(terminal, nodeInformation, LP.Random.Crypto.NextUInt64())
    {// NodeInformation: Managed
    }

    public override async Task<NetInterfaceResult> EncryptConnectionAsync(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (this.IsEncrypted)
        {// Encrypted
            return NetInterfaceResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return NetInterfaceResult.NoNodeInformation;
        }

        using (var operation = this.CreateOperation())
        {
            return await operation.EncryptConnectionAsync(millisecondsToWait).ConfigureAwait(false);
        }
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

        this.CreateHeader(out var header, this.GetSequential());
        header.Id = PacketId.Close;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.Endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
    }

    public async Task<NetInterfaceResult> SendPacketAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        using (var operation = this.CreateOperation())
        {
            return await operation.SendPacketAsync(value, millisecondsToWait).ConfigureAwait(false);
        }
    }

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {
        using (var operation = this.CreateOperation())
        {
            return await operation.SendPacketAndReceiveAsync<TSend, TReceive>(value, millisecondsToWait).ConfigureAwait(false);
        }
    }

    public async Task<NetInterfaceResult> SendAsync<TSend>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        using (var operation = this.CreateOperation())
        {
            return await operation.SendAsync(value, millisecondsToWait);
        }
    }

    public async Task<NetInterfaceResult> SendDataAsync(ulong dataId, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        using (var operation = this.CreateOperation())
        {
            return await operation.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
        }
    }

    public async Task<(NetInterfaceResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {// checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendAndReceiveAsync<TSend, TReceive>(value, millisecondsToWait);
        }
    }

    public async Task<(NetInterfaceResult Result, ReadOnlyMemory<byte> Value)> SendAndReceiveDataAsync(ulong dataId, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        using (var operation = this.CreateOperation())
        {
            var response = await operation.SendAndReceiveDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);
            return (response.Result, response.Received);
        }
    }
}
