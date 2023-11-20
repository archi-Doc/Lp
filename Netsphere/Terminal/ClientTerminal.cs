// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;

namespace Netsphere;

public class ClientTerminal : NetTerminalObsolete
{
    internal ClientTerminal(Terminal terminal, NetEndPoint endPoint)
        : base(terminal, endPoint)
    {// NodeAddress: Unmanaged
    }

    internal ClientTerminal(Terminal terminal, NetEndPoint endPoint, NetNode node)
        : base(terminal, endPoint, node, RandomVault.Crypto.NextUInt64())
    {// NodeInformation: Managed
    }

    public override async Task<NetResult> EncryptConnectionAsync()
    {// Checked
        if (this.IsEncrypted)
        {// Encrypted
            return NetResult.Success;
        }
        else if (this.Node == null && !this.Terminal.NetBase.AllowUnsafeConnection)
        {// Unmanaged
            return NetResult.NoNodeInformation;
        }

        using (var operation = this.CreateOperation())
        {
            return await operation.EncryptConnectionAsync().ConfigureAwait(false);
        }
    }

    public unsafe override void SendClose()
    {// Checked
        if (this.IsClosed)
        {
            return;
        }

        this.IsClosed = true;
        if (!this.IsEncrypted)
        {// Not encrypted (connected)
            return;
        }

        using (var operation = this.CreateOperation())
        {
            this.CreateHeader(out var header, operation.GetGene());
            header.Id = PacketId.Close;

            var arrayOwner = PacketPool.Rent();
            fixed (byte* bp = arrayOwner.ByteArray)
            {
                *(PacketHeader*)bp = header;
            }

            this.Terminal.AddRawSend(this.Endpoint.EndPoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize)); // nspi
            this.Logger?.Log("Send close (client)");
        }
    }

    public async Task<NetResult> SendPacketAsync<TSend>(TSend value)
        where TSend : IPacket
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendPacketAsync(value).ConfigureAwait(false);
        }
    }

    public async Task<(NetResult Result, TReceive? Value)> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value)
        where TSend : IPacket
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendPacketAndReceiveAsync<TSend, TReceive>(value).ConfigureAwait(false);
        }
    }

    public async Task<NetResult> SendAsync<TSend>(TSend value)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendAsync(value).ConfigureAwait(false);
        }
    }

    public async Task<NetResult> SendDataAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendDataAsync(true, PacketId.Data, dataId, data).ConfigureAwait(false);
        }
    }

    public async Task<NetResult> SendDataAsync(ulong dataId, byte[] data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data)).ConfigureAwait(false);
        }
    }

    public async Task<NetResult> SendServiceAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendDataAsync(true, PacketId.Rpc, dataId, data).ConfigureAwait(false);
        }
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceiveAsync<TSend, TReceive>(TSend value)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            return await operation.SendAndReceiveAsync<TSend, TReceive>(value).ConfigureAwait(false);
        }
    }

    public async Task<(NetResult Result, ByteArrayPool.MemoryOwner Value)> SendAndReceiveDataAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            var response = await operation.SendAndReceiveDataAsync(true, PacketId.Data, dataId, data).ConfigureAwait(false);
            return (response.Result, response.Received);
        }
    }

    public async Task<(NetResult Result, ulong DataId, ByteArrayPool.MemoryOwner Value)> SendAndReceiveDataAsync(ulong dataId, byte[] data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            var response = await operation.SendAndReceiveDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data)).ConfigureAwait(false);
            return (response.Result, response.DataId, response.Received);
        }
    }

    public async Task<(NetResult Result, ulong DataId, ByteArrayPool.MemoryOwner Value)> SendAndReceiveServiceAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        using (var operation = this.CreateOperation())
        {
            var response = await operation.SendAndReceiveDataAsync(true, PacketId.Rpc, dataId, data).ConfigureAwait(false);
            return (response.Result, response.DataId, response.Received);
        }
    }

    public TService GetService<TService>()
        where TService : INetService
    {
        return StaticNetService.CreateClient<TService>(this);
    }

    internal ClientOperation CreateOperation() => new ClientOperation(this);
}
