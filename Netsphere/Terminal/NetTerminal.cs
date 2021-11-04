// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;

namespace LP.Net;

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminal"/> class.
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
    internal NetTerminal(Terminal terminal, ulong gene, NodeAddress nodeAddress)
    {
        this.Terminal = terminal;
        this.Gene = gene;
        this.NodeAddress = nodeAddress;
        this.Endpoint = this.NodeAddress.CreateEndpoint();
    }

    public Terminal Terminal { get; }

    [Link(Type = ChainType.Ordered)]
    public ulong Gene { get; private set; }

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public IPEndPoint Endpoint { get; }

    public NodeAddress NodeAddress { get; }

    public unsafe void SendUnmanaged_Punch()
    {
        var p = new PacketPunch();
        p.UtcTicks = DateTime.UtcNow.Ticks;

        this.CreateHeader(out var header);
        var packet = PacketService.CreatePacket(ref header, p);
        this.SendPacket(packet, PacketId.PunchResponse);
    }

    public T? Receive<T>(int millisecondsToWait = DefaultMillisecondsToWait)
        where T : IPacket
    {
        var result = this.Receive(out var header, out var data, millisecondsToWait);
        if (!result || data.Length < PacketService.HeaderSize)
        {
            return default(T);
        }

        try
        {
            return TinyhandSerializer.Deserialize<T>(data);
        }
        catch
        {
            return default(T);
        }
    }

    internal bool Receive(out PacketId packetId, out Memory<byte> data, int millisecondsToWait = DefaultMillisecondsToWait)
    {
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                Log.Debug($"Receive timeout.");
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

    internal bool SendPacket(byte[] packet, PacketId responseId)
    {
        lock (this.syncObject)
        {
            if (this.genes != null)
            {
                return false;
            }

            var gene = new NetTerminalGene(this.Gene, this);
            gene.SetSend(packet, responseId);
            this.genes = new NetTerminalGene[] { gene, };
            this.Terminal.AddNetTerminalGene(this.genes);
        }

        return true;
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

    private protected unsafe bool ReceivePacket(out PacketId packetId, [MaybeNullWhen(false)] out Memory<byte> data)
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
