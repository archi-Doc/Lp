// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace LP.Net;

[ValueLinkObject]
public partial class NetTerminal : IDisposable
{
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

    public byte[]? Receive(int millisecondsToWait)
    {
        return null;
    }

    internal bool SendRaw(byte[] data)
    {
        lock (this.syncObject)
        {
            if (this.genes != null)
            {
                return false;
            }

            var gene = new NetTerminalGene(this.Gene, this);
            gene.State = NetTerminalGeneState.WaitingToSend;
            gene.Data = data;
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
                    if (x.State == NetTerminalGeneState.WaitingToSend && x.Data != null)
                    {
                        udp.Send(x.Data, this.EndPoint);
                        x.State = NetTerminalGeneState.WaitingForConfirmation;
                        x.InvokeTicks = currentTicks;
                    }
                }
            }
        }
    }

    internal bool ProcessRecv(NetTerminalGene netTerminalGene, IPEndPoint endPoint, ref PacketHeader header, Span<byte> data)
    {
        if (netTerminalGene.State == NetTerminalGeneState.WaitingForConfirmation)
        {
            if (!header.Id.IsResponse())
            {
                return false;
            }

            netTerminalGene.State = NetTerminalGeneState.ReceivedOrConfirmed;
        }

        return false;
    }

#pragma warning disable SA1307
#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? genes;
    // internal NetTerminalGene[]? sendGene;
    // internal NetTerminalGene[]? recvGene;
#pragma warning restore SA1401 // Fields should be private

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
