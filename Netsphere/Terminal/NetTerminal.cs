// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace LP.Net;

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
        this.GenePool = new(Random.Crypto.NextULong());
        this.NodeAddress = nodeAddress;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    internal NetTerminal(Terminal terminal, NodeInformation nodeInformation)
        : this(terminal, nodeInformation, Random.Crypto.NextULong())
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

    public Terminal Terminal { get; }

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public IPEndPoint Endpoint { get; }

    public NodeAddress NodeAddress { get; }

    public NodeInformation? NodeInformation { get; }

    public INetInterface<TSend> SendRaw<TSend>(TSend value)
        where TSend : IRawPacket
    {
        return this.SendPacket(value);
    }

    public enum SendResult
    {
        Success,
        Error,
        Timeout,
    }

    public SendResult CheckManagedAndEncrypted()
    {
        if (this.embryo != null)
        {// Encrypted
            return SendResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return SendResult.Error;
        }

        // var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        var p = new RawPacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        this.SendPacket(p);
        var r = this.ReceiveRaw<RawPacketEncrypt>();
        if (r != null)
        {
            if (this.CreateEmbryo(p.Salt))
            {
                return SendResult.Success;
            }
            else
            {
                return SendResult.Error;
            }
        }

        return SendResult.Timeout;
    }

    public SendResult Send<T>(T value, int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IRawPacket, IPacket
    {
        var result = this.CheckManagedAndEncrypted();
        if (result != SendResult.Success)
        {
            return result;
        }

        return this.SendPacket(value);
    }

    public T? ReceiveRaw<T>(int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IRawPacket
    {
        var result = this.Receive(out var data, millisecondsToWait);
        if (!result)
        {
            return default(T);
        }

        TinyhandSerializer.TryDeserialize<T>(data, out var value);
        return value;
    }

    internal GenePool GenePool { get; }

    internal bool Receive(out Memory<byte> data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        ulong recvGene;
        lock (this.syncObject)
        {
            recvGene = this.GenePool.GetGene();
            var gene = new NetTerminalGene(recvGene, this);
            gene.SetReceive();

            var index = this.EnsureReceive();
            this.recvGenes![index] = gene;
            this.Terminal.AddInbound(gene);
        }

        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Receive timeout.");
                goto ReceiveUnmanaged_Error;
            }

            lock (this.syncObject)
            {
                if (this.Terminal.TryGetInbound(recvGene, out var gene))
                {
                    if (gene.State == NetTerminalGeneState.Complete && !gene.ReceivedData.IsEmpty)
                    {
                        this.Terminal.RemoveInbound(gene);
                        data = gene.ReceivedData;
                        gene.Clear();
                        this.recvGenes = null;
                        return true;
                    }
                }
            }

            try
            {
                var cancelled = this.Terminal.Core?.CancellationToken.WaitHandle.WaitOne(1);
                if (cancelled != false)
                {
                    goto ReceiveUnmanaged_Error;
                }
            }
            catch
            {
                goto ReceiveUnmanaged_Error;
            }
        }

ReceiveUnmanaged_Error:
        data = default;
        return false;
    }

    internal void CreateHeader(out RawPacketHeader header, ulong gene)
    {
        header = default;
        header.Gene = gene;
        header.Engagement = this.NodeAddress.Engagement;
    }

    internal INetInterface<TSend> SendPacket<TSend>(TSend value)
        where TSend : IRawPacket
    {
        var netInterface = new NetInterface<TSend, object>();
        netInterface.Initialize(value);
        return netInterface;
    }

    internal unsafe SendResult RegisterSend(byte[] packet)
    {
        ulong headerGene;
        fixed (byte* pb = packet)
        {
            headerGene = (*(RawPacketHeader*)pb).Gene;
        }

        NetTerminalGene gene;
        lock (this.syncObject)
        {
            gene = new NetTerminalGene(headerGene, this);
            gene.SetSend(packet);
            var index = this.EnsureSend();
            this.sendGenes![index] = gene;
            this.Terminal.AddInbound(gene);
        }

        this.TerminalLogger?.Information($"RegisterSend   : {gene.ToString()}");

        return SendResult.Success;
    }

    internal unsafe SendResult RegisterReceive(int numberOfGenes)
    {
        lock (this.syncObject)
        {
            if (!this.PrepareReceive())
            {
                return SendResult.Error;
            }

            var gene = new NetTerminalGene(this.GenePool.GetGene(), this);
            gene.SetReceive();
            this.TerminalLogger?.Information($"RegisterReceive: {gene.ToString()}");
            this.recvGenes = new NetTerminalGene[] { gene, };
            this.Terminal.AddInbound(this.recvGenes);
        }

        return SendResult.Success;
    }

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        lock (this.syncObject)
        {
            if (this.sendGenes != null)
            {
                foreach (var x in this.sendGenes)
                {
                    if (x.State == NetTerminalGeneState.WaitingToSend)
                    {
                        if (x.Send(udp))
                        {
                            this.TerminalLogger?.Information($"Udp Sent       : {x.ToString()}");
                            x.SentTicks = currentTicks;
                        }
                    }
                }
            }
        }
    }

    internal void ProcessReceive(IPEndPoint endPoint, ref RawPacketHeader header, Memory<byte> data, long currentTicks, NetTerminalGene gene)
    {
        lock (this.syncObject)
        {
            if (this.recvGenes == null)
            {// No receive gene.
                this.TerminalLogger?.Error("No receive gene.");
                return;
            }

            if (!this.Endpoint.Equals(endPoint))
            {// Endpoint mismatch.
                this.TerminalLogger?.Error("Endpoint mismatch.");
                return;
            }

            if (header.Id == RawPacketId.Ack)
            {// Ack (header.Gene + data(ulong[]))
                gene.ReceiveAck();
                var g = MemoryMarshal.Cast<byte, ulong>(data.Span);
                this.TerminalLogger?.Information($"Recv Ack 1+{g.Length}");
                foreach (var x in g)
                {
                    if (this.Terminal.TryGetInbound(x, out var gene2))
                    {
                        if (gene2.NetTerminal == this)
                        {
                            gene2.ReceiveAck();
                        }
                    }
                }
            }
            else
            {// Receive data
                if (gene.Receive(data))
                {// Received.
                    this.TerminalLogger?.Information($"Recv data: {gene.ToString()}");
                }
            }
        }
    }

#pragma warning disable SA1307
#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? sendGenes;
    internal NetTerminalGene[]? recvGenes;
#pragma warning restore SA1401 // Fields should be private

    internal object syncInterfaceGene = new();
    internal ISimpleLogger? TerminalLogger => this.Terminal.TerminalLogger;

    internal bool CreateEmbryo(ulong salt)
    {
        Logger.Default.Information($"Salt {salt.ToString()}");
        if (this.NodeInformation == null)
        {
            return false;
        }

        var ecdh = NodeKey.FromPublicKey(this.NodeInformation.PublicKeyX, this.NodeInformation.PublicKeyY);
        if (ecdh == null)
        {
            return false;
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

        Logger.Default.Information($"embryo {this.embryo[0].ToString()}");
        this.GenePool.SetEmbryo(this.embryo);
        Logger.Default.Information($"First gene {this.GenePool.GetGene().ToString()}");

        return true;
    }

    private int EnsureSend()
    {
        if (this.sendGenes != null)
        {
            for (var i = 0; i < this.sendGenes.Length; i++)
            {
                if (this.sendGenes[i].IsAvailable)
                {// Available.
                    this.sendGenes[i].Clear();
                    return i;
                }
            }

            var originalLength = this.sendGenes.Length;
            Array.Resize<NetTerminalGene>(ref this.sendGenes, this.sendGenes.Length + 1);
            return originalLength;
        }
        else
        {
            this.sendGenes = new NetTerminalGene[1];
            return 0;
        }
    }

    private int EnsureReceive()
    {
        if (this.recvGenes != null)
        {
            for (var i = 0; i < this.recvGenes.Length; i++)
            {
                if (this.recvGenes[i].IsAvailable)
                {// Available.
                    this.recvGenes[i].Clear();
                    return i;
                }
            }

            var originalLength = this.recvGenes.Length;
            Array.Resize<NetTerminalGene>(ref this.recvGenes, this.recvGenes.Length + 1);
            return originalLength;
        }
        else
        {
            this.recvGenes = new NetTerminalGene[1];
            return 0;
        }
    }

    private bool PrepareReceive()
    {
        if (this.recvGenes != null)
        {
            foreach (var x in this.recvGenes)
            {
                if (!x.IsAvailable)
                {// Not available.
                    return false;
                }
            }

            foreach (var x in this.recvGenes)
            {
                x.Clear();
            }

            this.recvGenes = null;
        }

        return true;
    }

    private unsafe bool ReceivePacket(out Memory<byte> data)
    {
        if (this.recvGenes == null)
        {
            goto ReceivePacket_Error;
        }
        else if (this.recvGenes.Length == 0)
        {
            goto ReceivePacket_Error;
        }
        else if (this.recvGenes.Length == 1)
        {
            if (this.recvGenes[0].State == NetTerminalGeneState.Complete && !this.recvGenes[0].ReceivedData.IsEmpty)
            {
                data = this.recvGenes[0].ReceivedData;
                this.recvGenes[0].Clear();
                this.recvGenes = null;
                return true;
            }
        }
        else
        {
            foreach (var x in this.recvGenes)
            {
            }
        }

ReceivePacket_Error:
        data = default;
        return false;
    }

    private void ClearGenes()
    {
        lock (this.syncObject)
        {
            if (this.sendGenes != null)
            {
                this.Terminal.RemoveInbound(this.sendGenes);
                foreach (var x in this.sendGenes)
                {
                    x.Clear();
                }

                this.sendGenes = null;
            }

            if (this.recvGenes != null)
            {
                this.Terminal.RemoveInbound(this.recvGenes);
                foreach (var x in this.recvGenes)
                {
                    x.Clear();
                }

                this.recvGenes = null;
            }
        }
    }

    private object syncObject = new();

    private byte[]? embryo;

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
                this.ClearGenes();
                this.Terminal.TryRemove(this);
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
