// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Net;

public enum NetInterfaceReceiveResult
{
    Success,
    Timeout,
    Shutdown,
    DeserializeError,
}

public interface INetInterface<TSend, TReceive> : INetInterface<TSend>
{
    public NetInterfaceReceiveResult Receive(out TReceive? value, int millisecondsToWait = DefaultMillisecondsToWait);
}

public interface INetInterface<TSend>
{
    public const int DefaultMillisecondsToWait = 2000;

    public NetInterfaceReceiveResult WaitForSendCompletion(int millisecondsToWait = DefaultMillisecondsToWait);
}

internal class NetInterface<TSend, TReceive> : NetInterface, INetInterface<TSend, TReceive>
{
    public NetInterface(NetTerminal netTerminal)
        : base(netTerminal)
    {
    }

    public NetInterfaceReceiveResult Receive(out TReceive? value, int millisecondsToWait = 2000)
    {
        var result = this.ReceiveData(out var data, millisecondsToWait);
        if (result != NetInterfaceReceiveResult.Success)
        {
            value = default;
            return result;
        }

        TinyhandSerializer.TryDeserialize<TReceive>(data, out value);
        if (value == null)
        {
            return NetInterfaceReceiveResult.DeserializeError;
        }

        return result;
    }

    public NetInterfaceReceiveResult WaitForSendCompletion(int millisecondsToWait = 2000)
    {
        throw new NotImplementedException();
    }

    internal void Initialize(TSend value, RawPacketId id, bool receive)
    {
        var gene = this.NetTerminal.GenePool.GetGene(); // Send gene
        this.NetTerminal.CreateHeader(out var header, gene);
        var packet = PacketService.CreatePacket(ref header, value, id);
        if (packet.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
            var ntg = new NetTerminalGene(gene, this);
            ntg.SetSend(packet);
            this.sendGenes = new NetTerminalGene[] { ntg, };
            this.NetTerminal.Terminal.AddInbound(ntg);

            this.NetTerminal.TerminalLogger?.Information($"RegisterSend   : {gene.ToString()}");
        }
        else
        {// Split into multiple packets.
        }

        gene = this.NetTerminal.GenePool.GetGene(); // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(gene, this);
            ntg.SetReceive();
            this.recvGenes = new NetTerminalGene[] { ntg, };
            this.NetTerminal.Terminal.AddInbound(ntg);

            this.NetTerminal.TerminalLogger?.Information($"RegisterReceive: {gene.ToString()}");
        }
    }
}

internal class NetInterface
{
    public NetInterface(NetTerminal netTerminal)
    {
        this.NetTerminal = netTerminal;
    }

    public NetTerminal NetTerminal { get; }

    protected NetInterfaceReceiveResult ReceiveData(out Memory<byte> data, int millisecondsToWait)
    {
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.NetTerminal.Terminal.Core?.IsTerminated == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.NetTerminal.TerminalLogger?.Information($"Receive timeout.");
                goto ReceiveUnmanaged_Error;
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.NetTerminal.Terminal.TryGetInbound(0, out var gene))
                {
                    if (gene.State == NetTerminalGeneState.Complete && !gene.ReceivedData.IsEmpty)
                    {
                        this.NetTerminal.Terminal.RemoveInbound(gene);
                        data = gene.ReceivedData;
                        gene.Clear();
                        this.recvGenes = null;
                        return NetInterfaceReceiveResult.Success;
                    }
                }
            }

            try
            {
                var cancelled = this.NetTerminal.Terminal.Core?.CancellationToken.WaitHandle.WaitOne(1);
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
        return NetInterfaceReceiveResult.Timeout;
    }

#pragma warning disable SA1401 // Fields should be private
    protected NetTerminalGene[]? sendGenes;
    protected NetTerminalGene[]? recvGenes;
#pragma warning restore SA1401 // Fields should be private
}
