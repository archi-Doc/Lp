﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public bool IsManaged => this.NodeInformation != null;

    public enum SendResult
    {
        Success,
        Error,
        Timeout,
    }

    public SendResult SendUnmanaged<T>(T value)
        where T : IPacket
    {
        return this.SendPacket(value);
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
        var p = new PacketEncrypt(this.Terminal.NetStatus.GetMyNodeInformation());
        this.SendPacket(p, PacketId.Encrypt);
        var r = this.Receive<PacketEncrypt>();
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

    public SendResult Send<T>(T value, PacketId responseId, int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IPacket
    {
        var result = this.CheckManagedAndEncrypted();
        if (result != SendResult.Success)
        {
            return result;
        }

        return this.SendPacket(value, responseId);
    }

    public T? Receive<T>(int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IPacket
    {
        var result = this.Receive(out var header, out var data, millisecondsToWait);
        if (!result)
        {
            return default(T);
        }

        TinyhandSerializer.TryDeserialize<T>(data, out var value);
        return value;
    }

    internal GenePool GenePool { get; }

    internal bool Receive(out PacketId packetId, out Memory<byte> data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
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
                if (this.genes == null)
                {
                    goto ReceiveUnmanaged_Error;
                }

                if (this.ReceivePacket(out packetId, out data))
                {// Received
                    return true;
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
        packetId = default;
        data = default;
        return false;
    }

    internal void CreateHeader(out PacketHeader header, ulong gene)
    {
        header = default;
        header.Gene = gene;
        header.Engagement = this.NodeAddress.Engagement;
    }

    internal SendResult SendPacket<T>(T value)
        where T : IPacket
    {
        var gene = this.GenePool.GetGene();
        // Logger.Default.Information($"Send: {gene.ToString()}");

        this.CreateHeader(out var header, gene);
        var packet = PacketService.CreatePacket(ref header, value);
        return this.RegisterSend(packet);
    }

    internal unsafe SendResult RegisterSend(byte[] packet)
    {
        lock (this.syncObject)
        {
            if (this.sendGenes != null)
            {
                return SendResult.Error;
            }

            ulong headerGene;
            fixed (byte* pb = packet)
            {
                headerGene = (*(PacketHeader*)pb).Gene;
            }

            // Logger.Default.Information($"Recv: {g.ToString()}");
            var gene = new NetTerminalGene(headerGene, this);
            gene.SetSend(packet, responseId);
            this.genes = new NetTerminalGene[] { gene, };
            this.Terminal.AddNetTerminalGene(this.genes);
        }

        return SendResult.Success;
    }

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        lock (this.syncObject)
        {
            if (this.genes != null)
            {
                foreach (var x in this.genes)
                {
                    if (x.State == NetTerminalGeneState.WaitingToSend)
                    {
                        x.Send(udp);
                    }
                }
            }
        }
    }

#pragma warning disable SA1307
#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? sendGenes;
    internal NetTerminalGene[]? recvGenes;
#pragma warning restore SA1401 // Fields should be private

    internal Serilog.ILogger? TerminalLogger => this.Terminal.TerminalLogger;

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

    private unsafe bool ReceivePacket(out PacketId packetId, out Memory<byte> data)
    {
        if (this.genes == null)
        {
            goto ReceivePacket_Error;
        }
        else if (this.genes.Length == 0)
        {
            goto ReceivePacket_Error;
        }
        else if (this.genes.Length == 1)
        {
            if (this.genes[0].State == NetTerminalGeneState.ReceivedOrConfirmed && !this.genes[0].ReceivedData.IsEmpty)
            {
                packetId = this.genes[0].PacketId;
                data = this.genes[0].ReceivedData;

                this.genes = null;
                return true;
            }
        }
        else
        {
            foreach (var x in this.genes)
            {
            }
        }

ReceivePacket_Error:
        packetId = default;
        data = default;
        return false;
    }

    private void ClearGenes()
    {
        if (this.genes != null)
        {
            this.Terminal.RemoveNetTerminalGene(this.genes);
            foreach (var x in this.genes)
            {
                x.Clear();
            }

            this.genes = null;
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
                // lock (this.Terminal.terminals)
                {
                    this.Goshujin = null;
                    // this.terminals.Add(terminal);
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
