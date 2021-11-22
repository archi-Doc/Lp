// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Net;

public enum NetInterfaceResult
{
    Success,
    Timeout,
    Shutdown,
}

public interface INetInterface<TSend, TReceive> : INetInterface<TSend>
{
    public NetInterfaceResult TryReceive<TReseive>(out TReceive? value, int millisecondsToWait = DefaultMillisecondsToWait);
}

public interface INetInterface<TSend>
{
    public const int DefaultMillisecondsToWait = 2000;

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = DefaultMillisecondsToWait);
}

internal class NetInterface<TSend, TReceive> : NetInterface, INetInterface<TSend, TReceive>
{
    public NetInterface(NetTerminal netTerminal)
        : base(netTerminal)
    {
    }

    public NetInterfaceResult TryReceive<TReseive>(out TReceive? value, int millisecondsToWait = 2000)
    {
        throw new NotImplementedException();
    }

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = 2000)
    {
        throw new NotImplementedException();
    }

    internal void Initialize(TSend value, bool receive)
    {
        var gene = this.NetTerminal.GenePool.GetGene(); // Send gene
        this.NetTerminal.CreateHeader(out var header, gene);
        var packet = PacketService.CreatePacket(ref header, value);
        if (packet.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
            var ntg = new NetTerminalGene(gene, this);
            ntg.SetSend(packet);
            this.sendGenes = new NetTerminalGene[] { ntg, };
            this.NetTerminal.Terminal.AddInbound(ntg);
        }
        else
        {// Split into multiple packets.
        }

        this.NetTerminal.TerminalLogger?.Information($"RegisterSend   : {gene.ToString()}");

        gene = this.NetTerminal.GenePool.GetGene(); // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(gene, this);
            ntg.SetReceive();
            this.recvGenes = new NetTerminalGene[] { ntg, };
            this.NetTerminal.Terminal.AddInbound(ntg);
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

#pragma warning disable SA1401 // Fields should be private
    protected NetTerminalGene[]? sendGenes;
    protected NetTerminalGene[]? recvGenes;
#pragma warning restore SA1401 // Fields should be private
}
