// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace LP.Net;

[ValueLinkObject]
public partial class NetTerminal : IDisposable
{
    public const int DefaultMillisecondsToWait = 2000;
    /*internal struct Packet
    {
        public Packet(byte[] data)
        {
            this.Data = data;
        }

        public long CreatedTicks { get; } = Ticks.GetCurrent();

        public byte[] Data { get; }
    }*/

    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    internal NetTerminal(Terminal terminal, ulong gene, NodeAddress nodeAddress)
    {
        this.Terminal = terminal;
        this.Gene = gene;
        this.NodeAddress = nodeAddress;
        this.EndPoint = this.NodeAddress.CreateEndPoint();
    }

    public Terminal Terminal { get; }

    [Link(Type = ChainType.Ordered)]
    public ulong Gene { get; private set; }

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public IPEndPoint EndPoint { get; }

    public NodeAddress NodeAddress { get; }

    public unsafe void SendPunch()
    {
        var buffer = new byte[PacketHelper.HeaderSize];
        PacketHelper.SetHeader(buffer, this.Gene, PacketId.Punch);
        this.SendRaw(buffer);
    }

    public T? Receive<T>(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        var b = this.Receive(millisecondsToWait);
        if (b == null)
        {
            return default(T);
        }

        try
        {
            return TinyhandSerializer.Deserialize<T>(b);
        }
        catch
        {
            return default(T);
        }
    }

    public byte[]? Receive(int millisecondsToWait = DefaultMillisecondsToWait)
    {
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                return null;
            }

            lock (this.syncObject)
            {
                if (this.genes == null)
                {
                    return null;
                }

                var b = this.ReceiveData();
                if (b != null)
                {
                    return b;
                }
            }

            try
            {
                var cancelled = this.Terminal.Core?.CancellationToken.WaitHandle.WaitOne(1);
                if (cancelled != false)
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    internal bool SendRaw(byte[] packet)
    {
        lock (this.syncObject)
        {
            if (this.genes != null)
            {
                return false;
            }

            var gene = new NetTerminalGene(this.Gene, this);
            gene.State = NetTerminalGeneState.WaitingToSend;
            gene.Packet = packet;
            this.genes = new NetTerminalGene[] { gene, };
            this.Terminal.AddNetTerminalGene(this.genes);

            /*if (this.sendGene != null || this.recvGene != null)
            {
                return false;
            }

            var send = new NetTerminalGene(this.Gene, this);
            send.Data = data;
            this.sendGene = new NetTerminalGene[] { send, };

            var recv = new NetTerminalGene(this.Gene, this);
            this.recvGene = new NetTerminalGene[] { recv, };
            this.Terminal.AddRecvGene(this.recvGene);*/
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
                    if (x.State == NetTerminalGeneState.WaitingToSend && x.Packet != null)
                    {
                        udp.Send(x.Packet, this.EndPoint);
                        x.State = NetTerminalGeneState.WaitingForConfirmation;
                        x.InvokeTicks = currentTicks;
                    }
                }
            }
        }
    }

    internal bool ProcessRecv(NetTerminalGene netTerminalGene, IPEndPoint endPoint, ref PacketHeader header, byte[] packet)
    {
        if (netTerminalGene.State == NetTerminalGeneState.WaitingForConfirmation)
        {
            if (!header.Id.IsResponse())
            {
                return false;
            }

            netTerminalGene.State = NetTerminalGeneState.ReceivedOrConfirmed;
            netTerminalGene.Packet = packet;
        }

        return false;
    }

#pragma warning disable SA1307
#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? genes;
    // internal NetTerminalGene[]? sendGene;
    // internal NetTerminalGene[]? recvGene;
#pragma warning restore SA1401 // Fields should be private

    private byte[]? ReceiveData()
    {
        if (this.genes == null)
        {
            return null;
        }
        else if (this.genes.Length == 0)
        {
            return Array.Empty<byte>();
        }
        else if (this.genes.Length == 1)
        {
            if (this.genes[0].State == NetTerminalGeneState.ReceivedOrConfirmed)
            {
                return this.genes[0].Packet;
            }
        }

        foreach (var x in this.genes)
        {
        }

        return null;
    }

    private object syncObject = new();

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
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
