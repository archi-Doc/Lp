// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class ServerOperation : NetOperation
{
    internal ServerOperation(NetTerminal netTerminal)
        : base(netTerminal)
    {
        this.receiverInterface = NetInterface<object, byte[]>.CreateReceive(this);
    }

    public async Task<NetResult> SendEmpty()
    {// Checked
        return await this.SendDataAsync(0, Array.Empty<byte>()).ConfigureAwait(false);
    }

    public async Task<NetResult> SendDataAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        return await this.SendDataAsync(true, PacketId.Data, dataId, data).ConfigureAwait(false);
    }

    public async Task<NetResult> SendDataAsync(ulong dataId, byte[] data)
    {// Checked
        return await this.SendDataAsync(true, PacketId.Data, dataId, new ByteArrayPool.MemoryOwner(data)).ConfigureAwait(false);
    }

    public async Task<NetResult> SendServiceAsync(ulong dataId, ByteArrayPool.MemoryOwner data)
    {// Checked
        return await this.SendDataAsync(true, PacketId.Rpc, dataId, data).ConfigureAwait(false);
    }

    public unsafe void SendClose()
    {// Checked
        if (this.NetTerminal.IsClosed)
        {
            return;
        }

        this.NetTerminal.IsClosed = true;
        if (!this.NetTerminal.IsEncrypted)
        {// Not encrypted (connected)
            return;
        }

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

        this.Terminal.AddRawSend(this.NetTerminal.Endpoint.EndPoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize)); // nspi
    }

    public async Task<NetReceivedData> ReceiveAsync()
    {// Checked
        if (this.receiverInterface == null)
        {
            throw new InvalidOperationException();
        }

        var received = await this.receiverInterface.ReceiveAsync().ConfigureAwait(false);
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
        if (!TinyhandSerializer.TryDeserialize<PacketReserve>(received.Received.Memory.Span, out var reserve))
        {
            received.Return();
            return new(NetResult.DeserializationError);
        }

        received.Return();
        this.receiverInterface2 = NetInterface<object, byte[]>.CreateReserve2(this, reserve);

        this.receiverInterface.SetSend(new PacketReserveResponse());

        received = await this.receiverInterface2.ReceiveAsync().ConfigureAwait(false);
        return received;
    }

    public async Task<NetResult> SendPacketAsync<TSend>(TSend value)
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
        return await netInterface.WaitForSendCompletionAsync().ConfigureAwait(false);
    }

    public async Task<NetResult> SendAsync<TSend>(TSend value)
    {// Checked
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return NetResult.SerializationError;
        }

        Task<NetResult> task;
        if (value is IPacket packet)
        {
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.PacketId, (ulong)packet.PacketId, owner);
        }
        else
        {
            var dataId = BlockService.GetId<TSend>();
            task = this.SendDataAsync(true, PacketId.Data, dataId, owner);
        }

        owner.Return();
        return await task.ConfigureAwait(false);
    }

    internal async Task<NetResult> SendDataAsync(bool encrypt, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner)
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
            return await netInterface.WaitForSendCompletionAsync().ConfigureAwait(false);
        }
        else if (owner.Memory.Length > BlockService.MaxBlockSize)
        {// Block size limit exceeded.
            return NetResult.BlockSizeLimit;
        }

        // Split into multiple packets. Send PacketReserve.
        var reserve = new PacketReserve(owner.Memory.Length);
        netInterface.SetSend(reserve);

        netInterface = NetInterface<object, byte[]>.CreateReceive(this);
        try
        {
            var received = await netInterface.ReceiveAsync().ConfigureAwait(false);
            if (received.Result != NetResult.Success)
            {// Timeout/Error
                return received.Result;
            }
            else if (received.PacketId != PacketId.ReserveResponse)
            {
                return NetResult.ReserveError;
            }

            netInterface.SetSend(this, packetId, dataId, owner);
            return await netInterface.WaitForSendCompletionAsync().ConfigureAwait(false);
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
