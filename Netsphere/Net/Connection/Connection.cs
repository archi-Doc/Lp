// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Netsphere.Packet;
using Netsphere.Transmission;

#pragma warning disable SA1202

namespace Netsphere;

// byte[32] Key, byte[16] Iv
internal readonly record struct Embryo(ulong Salt, byte[] Key, byte[] Iv);

public abstract class Connection : IDisposable
{
    public enum ConnectMode
    {
        ReuseClosed,
        ReuseOpen,
        NoReuse,
    }

    public enum ConnectionState
    {
        Open,
        Closed,
        Disposed,
    }

    public Connection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
    {
        this.NetBase = connectionTerminal.NetBase;
        this.packetTerminal = packetTerminal;
        this.connectionTerminal = connectionTerminal;
        this.ConnectionId = connectionId;
        this.EndPoint = endPoint;
    }

    public void Close()
        => this.Dispose();

    internal void Initialize(Embryo embryo)
    {
        this.embryo = embryo;
        this.aes = Aes.Create();
        this.aes.KeySize = 256;
        this.aes.Key = this.embryo.Key;
    }

    #region FieldAndProperty

    public NetBase NetBase { get; }

    public ulong ConnectionId { get; }

    public NetEndPoint EndPoint { get; }

    public abstract ConnectionState State { get; }

    internal long ClosedSystemMics { get; set; }

    private readonly PacketTerminal packetTerminal;
    private readonly ConnectionTerminal connectionTerminal;
    private Embryo embryo;
    private Aes aes = default!;
    private SendTransmission.GoshujinClass sendTransmissions = new();

    #endregion

    internal void SendPriorityFrame(scoped Span<byte> frame)
    {// Close, Ack
        if (!this.CreatePacket(frame, out var owner))
        {
            return;
        }

        this.packetTerminal.AddSendPacket(this.EndPoint.EndPoint, owner, false, default);
    }

    internal void SendCloseFrame()
    {// Close, Ack
        if (!this.CreatePacket(Array.Empty<byte>(), out var owner))
        {
            return;
        }

        this.packetTerminal.AddSendPacket(this.EndPoint.EndPoint, owner, false, default);
    }

    private bool CreatePacket(scoped Span<byte> frame, out ByteArrayPool.MemoryOwner owner)
    {
        if (frame.Length > PacketHeader.MaxFrameLength)
        {
            owner = default;
            return false;
        }

        var arrayOwner = PacketPool.Rent();
        var span = arrayOwner.ByteArray.AsSpan();
        var salt = RandomVault.Pseudo.NextUInt64();

        // PacketHeaderCode
        BitConverter.TryWriteBytes(span, salt); // Salt
        span = span.Slice(sizeof(uint));

        BitConverter.TryWriteBytes(span, (ushort)this.EndPoint.Engagement); // Engagement
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, (ushort)PacketType.Encrypted); // PacketType
        span = span.Slice(sizeof(ushort));

        BitConverter.TryWriteBytes(span, this.ConnectionId); // Id
        span = span.Slice(sizeof(ulong));

        Span<byte> iv = stackalloc byte[16];
        this.embryo.Iv.CopyTo(iv);
        BitConverter.TryWriteBytes(iv, salt);

        if (!this.aes.TryEncryptCbc(frame, iv, span, out var written, PaddingMode.PKCS7))
        {
            owner = default;
            return false;
        }

        owner = arrayOwner.ToMemoryOwner(0, PacketHeader.Length + written);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.connectionTerminal.CloseInternal(this, false);
    }

    internal void DisposeActual()
    {// lock (this.Goshujin.SyncObject)
        if (this.State == ConnectionState.Open)
        {
            this.SendCloseFrame();
        }

        this.aes?.Dispose();

        // tempcode
        // this.sendTransmissions.Dispose();
    }
}
