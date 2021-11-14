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
        this.Gene = Random.Crypto.NextULong();
        this.NodeAddress = nodeAddress;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    internal NetTerminal(Terminal terminal, NodeInformation nodeInformation)
    {// NodeInformation: Managed
        this.Terminal = terminal;
        this.Gene = Random.Crypto.NextULong();
        this.NodeAddress = nodeInformation;
        this.NodeInformation = nodeInformation;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    public Terminal Terminal { get; }

    public ulong Gene { get; private set; }

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public IPEndPoint Endpoint { get; }

    public NodeAddress NodeAddress { get; }

    public NodeInformation? NodeInformation { get; }

    public bool IsManaged => this.NodeInformation != null;

    public unsafe void SendUnmanaged_Punch()
    {
        var p = new PacketPunch(null);
        this.SendPacket(p, PacketId.PunchResponse);
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
        {
            return SendResult.Success;
        }
        else if (this.NodeInformation == null)
        {// Unmanaged
            return SendResult.Error;
        }

        var p = new PacketEncrypt(this.NodeInformation);
        this.SendPacket(p, PacketId.Encrypt);
        var r = this.Receive<PacketEncrypt>();
        if (r != null && this.CreateEmbryo(p.Salt))
        {
            return SendResult.Success;
        }

        return SendResult.Timeout;
    }

    public SendResult Send<T>(T value, int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IPacket
    {
        var result = this.CheckManagedAndEncrypted();
        if (result != SendResult.Success)
        {
            return result;
        }

        return SendResult.Success;
    }

    public T? Receive<T>(int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IPacket
    {
        var result = this.Receive(out var header, out var data, millisecondsToWait);
        if (!result || data.Length < PacketService.HeaderSize)
        {
            return default(T);
        }

        TinyhandSerializer.TryDeserialize<T>(data, out var value);
        return value;
    }

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

    internal void CreateHeader(out PacketHeader header)
    {
        header = default;
        header.Gene = this.Gene;
        header.Engagement = this.NodeAddress.Engagement;
    }

    internal SendResult SendPacket<T>(T value, PacketId responseId)
        where T : IPacket
    {
        this.CreateHeader(out var header);
        var packet = PacketService.CreatePacket(ref header, value);
        return this.SendPacket(packet, responseId);
    }

    internal SendResult SendPacket(byte[] packet, PacketId responseId)
    {
        lock (this.syncObject)
        {
            if (this.genes != null)
            {
                return SendResult.Error;
            }

            var gene = new NetTerminalGene(this.Gene, this);
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

    internal bool ProcessRecv(NetTerminalGene netTerminalGene, IPEndPoint endPoint, ref PacketHeader header, Memory<byte> data)
    {
        if (netTerminalGene.Receive(data))
        {
        }

        return false;
    }

#pragma warning disable SA1307
#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? genes;
    // internal NetTerminalGene[]? sendGene;
    // internal NetTerminalGene[]? recvGene;
#pragma warning restore SA1401 // Fields should be private

    internal Serilog.ILogger? TerminalLogger => this.Terminal.TerminalLogger;

    private protected unsafe bool ReceivePacket(out PacketId packetId, out Memory<byte> data)
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
                if (this.genes[0].ReceivedData.Length < PacketService.HeaderSize)
                {
                    goto ReceivePacket_Error;
                }

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

    private bool CreateEmbryo(ulong salt)
    {
        if (this.NodeInformation == null)
        {
            return false;
        }

        var ecdh = NodeKey.FromPublicKey(this.NodeInformation.PublicKeyX, this.NodeInformation.PublicKeyY);
        var material = this.Terminal.Private.NodePrivateEcdh.DeriveKeyMaterial(ecdh.PublicKey);
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

        return true;
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
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
