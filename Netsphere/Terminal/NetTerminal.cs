// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminal"/> class.<br/>
/// NOT thread-safe.
/// </summary>
[ValueLinkObject]
public partial class NetTerminal : IDisposable
{
    public const int DefaultMillisecondsToWait = 2000;

    /// <summary>
    /// The default interval time in milliseconds.
    /// </summary>
    public const int DefaultInterval = 10;

    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    internal NetTerminal(Terminal terminal, NodeAddress nodeAddress)
    {// NodeAddress: Unmanaged
        this.Terminal = terminal;
        this.GenePool = new(LP.Random.Crypto.NextULong());
        this.NodeAddress = nodeAddress;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    internal NetTerminal(Terminal terminal, NodeInformation nodeInformation)
        : this(terminal, nodeInformation, LP.Random.Crypto.NextULong())
    {// NodeInformation: Managed
    }

    internal NetTerminal(Terminal terminal, NodeInformation nodeInformation, ulong gene)
    {// NodeInformation: Encrypted
        this.Terminal = terminal;
        this.GenePool = new(gene);
        this.NodeAddress = nodeInformation;
        this.NodeInformation = nodeInformation;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    // public virtual NetInterfaceResult EncryptConnection() => NetInterfaceResult.NoEncryptedConnection;

    public virtual async Task<NetInterfaceResult> EncryptConnectionAsync() => NetInterfaceResult.NoEncryptedConnection;

    /*public INetInterface<TSend> SendPacket<TSend>(TSend value)
        where TSend : IPacket
    {// checked
        if (!value.AllowUnencrypted)
        {
            var result = this.EncryptConnection();
            if (result != NetInterfaceResult.Success)
            {
                return NetInterface<TSend, object>.CreateError(this, result);
            }
        }

        return this.CreateSendValue(value);
    }

    public INetInterface<TSend, TReceive> SendPacketAndReceive<TSend, TReceive>(TSend value)
        where TSend : IPacket
    {// checked
        if (!value.AllowUnencrypted)
        {
            var result = this.EncryptConnection();
            if (result != NetInterfaceResult.Success)
            {
                return (INetInterface<TSend, TReceive>)NetInterface<TSend, object>.CreateError(this, result);
            }
        }

        return this.CreateSendAndReceiveValue<TSend, TReceive>(value);
    }*/

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

    public async Task<TReceive?> SendPacketAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
        where TSend : IPacket
    {// checked
        if (!value.AllowUnencrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return default;
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

    public async Task<TReceive?> SendAndReceiveAsync<TSend, TReceive>(TSend value, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (!BlockService.TrySerialize(value, out var owner))
        {
            return default;
        }

        Task<byte[]?> task;
        if (value is IPacket packet)
        {
            task = this.SendAndReceiveDataAsync(!packet.AllowUnencrypted, packet.Id, 0, owner, millisecondsToWait);
        }
        else
        {
            task = this.SendAndReceiveDataAsync(true, PacketId.Data, 0, owner, millisecondsToWait);
        }

        owner.Return();

        TinyhandSerializer.TryDeserialize<TReceive>(task.Result, out var received);
        return received;
    }

    public async Task<byte[]?> SendAndReceiveDataAsync(uint id, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendAndReceiveDataAsync(true, PacketId.Data, id, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

    private async Task<byte[]?> SendAndReceiveDataAsync(bool encrypt, PacketId packetId, uint id, ByteArrayPool.MemoryOwner owner, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        if (encrypt)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetInterfaceResult.Success)
            {
                return default;
            }
        }

        var netInterface = this.CreateSendAndReceiveData(packetId, id, owner);
        try
        {
            return await netInterface.ReceiveDataAsync(millisecondsToWait).ConfigureAwait(false);
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
            return default;
        }

        Task<NetInterfaceResult> task;
        if (value is IPacket packet)
        {
            task = this.SendDataAsync(!packet.AllowUnencrypted, packet.Id, 0, owner, millisecondsToWait);
        }
        else
        {
            task = this.SendDataAsync(true, PacketId.Data, 0, owner, millisecondsToWait);
        }

        owner.Return();
        return task.Result;
    }

    public async Task<NetInterfaceResult> SendDataAsync(uint id, byte[] data, int millisecondsToWait = DefaultMillisecondsToWait)
        => await this.SendDataAsync(true, PacketId.Data, id, new ByteArrayPool.MemoryOwner(data), millisecondsToWait).ConfigureAwait(false);

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

    public Terminal Terminal { get; }

    public bool IsEncrypted => this.embryo != null;

    public bool IsSendComplete => false;

    public bool IsReceiveComplete => false;

    public bool IsHighTraffic => false;

    public bool IsClosed => this.disposed;

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public IPEndPoint Endpoint { get; }

    public NodeAddress NodeAddress { get; }

    public NodeInformation? NodeInformation { get; }

    internal GenePool GenePool { get; }

    internal void CreateHeader(out PacketHeader header, ulong gene)
    {
        header = default;
        header.Gene = gene;
        header.Engagement = this.NodeAddress.Engagement;
    }

    internal unsafe void SendAck(ulong gene)
    {
        this.CreateHeader(out var header, gene);
        header.Id = PacketId.Ack;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.Endpoint, arrayOwner.ToMemoryOwner());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NetInterface<TSend, object> CreateSendValue<TSend>(TSend value)
        where TSend : IPacket
    {
        return NetInterface<TSend, object>.CreateValue(this, value, value.Id, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NetInterface<TSend, TReceive> CreateSendAndReceiveValue<TSend, TReceive>(TSend value)
        where TSend : IPacket
        => NetInterface<TSend, TReceive>.CreateValue(this, value, value.Id, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NetInterface<byte[], object> CreateSendData(PacketId packetId, ulong id, ByteArrayPool.MemoryOwner sendOwner)
        => NetInterface<byte[], object>.CreateData(this, packetId, id, sendOwner, false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NetInterface<byte[], byte[]> CreateSendAndReceiveData(PacketId packetId, ulong id, ByteArrayPool.MemoryOwner sendOwner)
        => NetInterface<byte[], byte[]>.CreateData(this, packetId, id, sendOwner, true);

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        lock (this.SyncObject)
        {
            if (this.IsClosed)
            {
                return;
            }

            foreach (var x in this.netInterfaces)
            {
                x.ProcessSend(udp, currentTicks);
            }
        }
    }

    internal void Add(NetInterface netInterface)
    {
        lock (this.SyncObject)
        {
            this.netInterfaces.Add(netInterface);
        }
    }

    internal bool RemoveInternal(NetInterface netInterface)
    {// lock (this.SyncObject)
        return this.netInterfaces.Remove(netInterface);
    }

    internal object SyncObject { get; } = new();

    internal ISimpleLogger? TerminalLogger => this.Terminal.TerminalLogger;

    internal NetInterfaceResult CreateEmbryo(ulong salt)
    {
        if (this.NodeInformation == null)
        {
            return NetInterfaceResult.NoNodeInformation;
        }

        var ecdh = NodeKey.FromPublicKey(this.NodeInformation.PublicKeyX, this.NodeInformation.PublicKeyY);
        if (ecdh == null)
        {
            return NetInterfaceResult.NoNodeInformation;
        }

        var material = this.Terminal.NodePrivateECDH.DeriveKeyMaterial(ecdh.PublicKey);
        Span<byte> buffer = stackalloc byte[sizeof(ulong) + NodeKey.PrivateKeySize + sizeof(ulong)];
        var span = buffer;
        BitConverter.TryWriteBytes(span, salt);
        span = span.Slice(sizeof(ulong));
        material.AsSpan().CopyTo(span);
        span = span.Slice(NodeKey.PrivateKeySize);
        BitConverter.TryWriteBytes(span, salt);

        var sha = Hash.Sha3_384Pool.Get();
        this.embryo = sha.GetHash(buffer);
        Hash.Sha3_384Pool.Return(sha);

        this.GenePool.SetEmbryo(this.embryo);
        Logger.Priority.Information($"First gene {this.GenePool.GetGene().ToString()}");

        return NetInterfaceResult.Success;
    }

    private void Clear()
    {// lock (this.SyncObject)
        foreach (var x in this.netInterfaces)
        {
            x.Clear();
        }

        this.netInterfaces.Clear();
    }

    protected List<NetInterface> netInterfaces = new();
    protected byte[]? embryo;

    // private PacketService packetService = new();

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="NetTerminal"/> class.
    /// </summary>
    ~NetTerminal()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.Terminal.TryRemove(this);
                lock (this.SyncObject)
                {
                    this.Clear();
                }

                // this.TerminalLogger?.Information("terminal disposed.");
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
