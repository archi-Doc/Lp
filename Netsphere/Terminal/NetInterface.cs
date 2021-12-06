// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

public enum NetInterfaceResult
{
    Success,
    Timeout,
    Closed,
    NoDataToSend,
    NoNodeInformation,
    NoEncryptedConnection,
    SerializationError,
    DeserializationError,
    PacketSizeLimit,
    BlockSizeLimit,
}

public interface INetInterface<TSend, TReceive> : INetInterface<TSend>
{
    public NetInterfaceResult Receive(out TReceive? value, int millisecondsToWait = DefaultMillisecondsToWait);
}

public interface INetInterface<TSend> : IDisposable
{
    public const int DefaultMillisecondsToWait = 2000;

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = DefaultMillisecondsToWait);
}

internal class NetInterface<TSend, TReceive> : NetInterface, INetInterface<TSend, TReceive>
{
    internal static NetInterface<TSend, TReceive> CreateError(NetTerminal netTerminal, NetInterfaceResult error)
    {// Error (Invalid NetInterface)
        if (error == NetInterfaceResult.Success)
        {
            throw new InvalidOperationException();
        }

        return new NetInterface<TSend, TReceive>(netTerminal, error);
    }

    internal static NetInterface<TSend, TReceive> CreateData(NetTerminal netTerminal, PacketId packetId, ulong id, ByteArrayPool.MemoryOwner owner, bool receive)
    {// Send and Receive(optional) NetTerminalGene.
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);

        var gene = netTerminal.GenePool.GetGene(); // Send gene
        netTerminal.CreateHeader(out var header, gene);
        if (owner.Memory.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
            PacketService.CreatePacket(ref header, packetId, id, owner.Memory.Span, out var sendOwner);

            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);
            sendOwner.Return();

            netTerminal.TerminalLogger?.Information($"RegisterSend2  : {gene.To4Hex()}");
        }
        else if (owner.Memory.Length <= BlockService.MaxBlockSize)
        {// Split into multiple packets.
            netInterface.SendGenes = CreateSendGenes(netInterface, gene, owner);
        }
        else
        {// Block size limit exceeded.
            netTerminal.GenePool.GetGene();
            return new NetInterface<TSend, TReceive>(netTerminal, NetInterfaceResult.BlockSizeLimit);
        }

        gene = netTerminal.GenePool.GetGene(); // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.RecvGenes = new NetTerminalGene[] { ntg, };
            ntg.SetReceive();

            netTerminal.TerminalLogger?.Information($"RegisterReceive2:{gene.To4Hex()}");
        }

        netTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetInterface<TSend, TReceive> CreateValue(NetTerminal netTerminal, TSend value, PacketId id, bool receive)
    {// Send and Receive(optional) NetTerminalGene.
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);

        var gene = netTerminal.GenePool.GetGene(); // Send gene
        netTerminal.CreateHeader(out var header, gene);
        PacketService.CreatePacket(ref header, value, id, out var sendOwner);
        if (sendOwner.Memory.Length <= PacketService.SafeMaxPacketSize)
        {// Single packet.
            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);

            netTerminal.TerminalLogger?.Information($"RegisterSend   : {gene.To4Hex()}");
        }
        else
        {// Packet size limit exceeded.
            sendOwner.Return();
            netTerminal.GenePool.GetGene();
            return new NetInterface<TSend, TReceive>(netTerminal, NetInterfaceResult.PacketSizeLimit);
        }

        gene = netTerminal.GenePool.GetGene(); // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.RecvGenes = new NetTerminalGene[] { ntg, };
            ntg.SetReceive();

            netTerminal.TerminalLogger?.Information($"RegisterReceive: {gene.To4Hex()}");
        }

        netTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetInterface<TSend, TReceive> CreateConnect(NetTerminal netTerminal, ulong gene, ByteArrayPool.MemoryOwner receiveOwner, ulong secondGene, ByteArrayPool.MemoryOwner sendOwner)
    {// Only for connection.
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);

        var recvGene = new NetTerminalGene(gene, netInterface);
        netInterface.RecvGenes = new NetTerminalGene[] { recvGene, };
        recvGene.SetReceive();
        recvGene.Receive(PacketId.Encrypt, receiveOwner);

        var sendGene = new NetTerminalGene(secondGene, netInterface);
        netInterface.SendGenes = new NetTerminalGene[] { sendGene, };
        sendGene.SetSend(sendOwner);

        netInterface.NetTerminal.TerminalLogger?.Information($"ConnectTerminal: {gene.To4Hex()} -> {secondGene.To4Hex()}");

        netInterface.NetTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetTerminalGene[] CreateSendGenes(NetInterface<TSend, TReceive> netInterface, ulong gene, ByteArrayPool.MemoryOwner owner)
    {
        ReadOnlySpan<byte> span = owner.Memory.Span;
        var netTerminal = netInterface.NetTerminal;
        var info = PacketService.GetDataInfo(owner.Memory.Length);

        var genes = new NetTerminalGene[info.NumberOfGenes];
        for (var i = 0; i < info.NumberOfGenes; i++)
        {
            var size = info.DataSize;
            if (i == (info.NumberOfGenes - 1))
            {
                size = info.LastDataSize;
            }

            netTerminal.CreateHeader(out var header, gene);
            PacketService.CreatePacket(ref header, PacketId.Data, 0, span.Slice(0, size), out var sendOwner);
            span = span.Slice(size);

            genes[i] = new(gene, netInterface);
            genes[i].SetSend(sendOwner);
            sendOwner.Return();
        }

        return genes;
    }

    protected NetInterface(NetTerminal netTerminal)
    : base(netTerminal)
    {
    }

    protected NetInterface(NetTerminal netTerminal, NetInterfaceResult error)
    : base(netTerminal, error)
    {
    }

    public NetInterfaceResult Receive(out TReceive? value, int millisecondsToWait = 2000)
    {
        var result = this.ReceiveCore(out var data, millisecondsToWait);
        if (result != NetInterfaceResult.Success)
        {
            value = default;
            return result;
        }

        TinyhandSerializer.TryDeserialize<TReceive>(data, out value);
        if (value == null)
        {
            return NetInterfaceResult.DeserializationError;
        }

        return result;
    }

    public async Task<(NetInterfaceResult Result, TReceive? Value)> ReceiveAsync(int millisecondsToWait = 2000)
    {
        var r = await this.ReceiveAsyncCore(millisecondsToWait).ConfigureAwait(false);
        if (r.Result != NetInterfaceResult.Success)
        {
            return (r.Result, default);
        }

        TinyhandSerializer.TryDeserialize<TReceive>(r.Received, out var value);
        if (value == null)
        {
            return (NetInterfaceResult.DeserializationError, default);
        }

        return (NetInterfaceResult.Success, value);
    }

    public async Task<(NetInterfaceResult Result, byte[]? Value)> ReceiveDataAsync(int millisecondsToWait = 2000)
    {
        var r = await this.ReceiveAsyncCore(millisecondsToWait).ConfigureAwait(false);
        if (r.Result != NetInterfaceResult.Success)
        {
            return (r.Result, default);
        }

        return (NetInterfaceResult.Success, r.Received.ToArray());
    }

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = 2000)
        => this.WaitForSendCompletionCore(millisecondsToWait);

    public Task<NetInterfaceResult> WaitForSendCompletionAsync(int millisecondsToWait = 2000)
        => this.WaitForSendCompletionAsyncCore(millisecondsToWait);
}

public class NetInterface : IDisposable
{
    public const int IntervalInMilliseconds = 2;

    protected NetInterface(NetTerminal netTerminal)
    {
        this.Terminal = netTerminal.Terminal;
        this.NetTerminal = netTerminal;
    }

    protected NetInterface(NetTerminal netTerminal, NetInterfaceResult error)
    {
        this.Terminal = netTerminal.Terminal;
        this.NetTerminal = netTerminal;
        this.Error = error;
    }

    public Terminal Terminal { get; }

    public NetTerminal NetTerminal { get; }

    public NetInterfaceResult Error { get; protected set; }

    protected NetInterfaceResult WaitForSendCompletionCore(int millisecondsToWait)
    {
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Send timeout.");
                return NetInterfaceResult.Timeout;
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.SendGenes == null)
                {
                    return NetInterfaceResult.NoDataToSend;
                }

                foreach (var x in this.SendGenes)
                {
                    if (!x.IsSent)
                    {
                        goto WaitForSendCompletionWait;
                    }
                }

                return NetInterfaceResult.Success;
            }

WaitForSendCompletionWait:
            try
            {
                var cancelled = this.Terminal.Core?.CancellationToken.WaitHandle.WaitOne(1);
                if (cancelled != false)
                {
                    return NetInterfaceResult.Closed;
                }
            }
            catch
            {
                return NetInterfaceResult.Closed;
            }
        }

        return NetInterfaceResult.Closed;
    }

    protected async Task<NetInterfaceResult> WaitForSendCompletionAsyncCore(int millisecondsToWait)
    {
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Send timeout.");
                return NetInterfaceResult.Timeout;
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.SendGenes == null)
                {
                    return NetInterfaceResult.NoDataToSend;
                }

                foreach (var x in this.SendGenes)
                {
                    if (!x.IsSent)
                    {
                        goto WaitForSendCompletionWait;
                    }
                }

                return NetInterfaceResult.Success;
            }

WaitForSendCompletionWait:
            try
            {
                var ct = this.Terminal.Core?.CancellationToken ?? CancellationToken.None;
                await Task.Delay(NetInterface.IntervalInMilliseconds, ct).ConfigureAwait(false);
            }
            catch
            {
                this.Error = NetInterfaceResult.Closed;
                return NetInterfaceResult.Closed;
            }
        }

        this.Error = NetInterfaceResult.Closed;
        return NetInterfaceResult.Closed;
    }

    protected NetInterfaceResult ReceiveCore(out ReadOnlyMemory<byte> data, int millisecondsToWait)
    {
        data = default;
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Receive timeout.");
                return NetInterfaceResult.Timeout;
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.ReceivedGeneToData(ref data))
                {
                    return NetInterfaceResult.Success;
                }
            }

            try
            {
                var cancelled = this.Terminal.Core?.CancellationToken.WaitHandle.WaitOne(1);
                if (cancelled != false)
                {
                    return NetInterfaceResult.Closed;
                }
            }
            catch
            {
                return NetInterfaceResult.Closed;
            }
        }

        return NetInterfaceResult.Closed;
    }

    protected async Task<(NetInterfaceResult Result, ReadOnlyMemory<byte> Received)> ReceiveAsyncCore(int millisecondsToWait)
    {
        ReadOnlyMemory<byte> data = default;
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Receive timeout.");
                return (NetInterfaceResult.Timeout, data);
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.ReceivedGeneToData(ref data))
                {
                    return (NetInterfaceResult.Success, data);
                }
            }

            try
            {
                var ct = this.Terminal.Core?.CancellationToken ?? CancellationToken.None;
                await Task.Delay(NetInterface.IntervalInMilliseconds, ct).ConfigureAwait(false);
            }
            catch
            {
                this.Error = NetInterfaceResult.Closed;
                return (NetInterfaceResult.Closed, data);
            }
        }

        return (NetInterfaceResult.Closed, data);
    }

    internal ISimpleLogger? TerminalLogger => this.Terminal.TerminalLogger;

    protected bool ReceivedGeneToData(ref ReadOnlyMemory<byte> data)
    {// lock (this.NetTerminal.SyncObject)
        if (this.RecvGenes == null)
        {// Empty
            return true;
        }
        else if (this.RecvGenes.Length == 1)
        {// Single gene
            if (!this.RecvGenes[0].IsReceived)
            {
                return false;
            }

            data = this.RecvGenes[0].Owner.Memory;
            return true;
        }

        // Multiple genes
        var total = 0;
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            if (!this.RecvGenes[i].IsReceived)
            {
                return false;
            }
            else
            {
                total += this.RecvGenes[i].Owner.Memory.Length;
            }
        }

        var buffer = new byte[total];
        var mem = buffer.AsMemory();
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            this.RecvGenes[i].Owner.Memory.CopyTo(mem);
            mem = mem.Slice(this.RecvGenes[i].Owner.Memory.Length);
        }

        data = buffer;
        return true;
    }

#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? SendGenes;
    internal NetTerminalGene[]? RecvGenes;
#pragma warning restore SA1401 // Fields should be private

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {// lock (this.NetTerminal.SyncObject)
        if (this.SendGenes != null)
        {
            foreach (var x in this.SendGenes)
            {
                if (x.State == NetTerminalGeneState.WaitingToSend)
                {
                    if (x.Send(udp))
                    {
                        this.TerminalLogger?.Information($"Udp Sent       : {x.ToString()}");
                        x.SentTicks = currentTicks;
                    }
                }
                else if (x.State == NetTerminalGeneState.WaitingForAck &&
                    (currentTicks - x.SentTicks) > Ticks.FromSeconds(0.5))
                {
                    if (x.Send(udp))
                    {
                        this.TerminalLogger?.Information($"Udp Resent     : {x.ToString()}");
                        x.SentTicks = currentTicks;
                    }
                }
            }
        }
    }

    internal void ProcessReceive(ByteArrayPool.MemoryOwner owner, IPEndPoint endPoint, ref PacketHeader header, long currentTicks, NetTerminalGene gene)
    {
        lock (this.NetTerminal.SyncObject)
        {
            if (this.NetTerminal.IsClosed)
            {
                return;
            }

            if (!this.NetTerminal.Endpoint.Equals(endPoint))
            {// Endpoint mismatch.
                this.TerminalLogger?.Error("Endpoint mismatch.");
                return;
            }

            if (header.Id == PacketId.Ack)
            {// Ack (header.Gene + data(ulong[]))
                gene.ReceiveAck();
                var g = MemoryMarshal.Cast<byte, ulong>(owner.Memory.Span);
                this.TerminalLogger?.Information($"Recv Ack 1+{g.Length}");
                foreach (var x in g)
                {
                    if (this.Terminal.TryGetInbound(x, out var gene2))
                    {
                        if (gene2.NetInterface.NetTerminal == this.NetTerminal)
                        {
                            gene2.ReceiveAck();
                        }
                    }
                }
            }
            else
            {// Receive data
                if (gene.Receive(header.Id, owner))
                {// Received.
                    this.TerminalLogger?.Information($"Recv data: {header.Id} {gene.ToString()}");
                }
            }
        }
    }

    internal void Clear()
    {// lock (this.NetTerminal.SyncObject)
        if (this.SendGenes != null)
        {
            foreach (var x in this.SendGenes)
            {
                x.Clear();
            }

            this.SendGenes = null;
        }

        if (this.RecvGenes != null)
        {
            foreach (var x in this.RecvGenes)
            {
                x.Clear();
            }

            this.RecvGenes = null;
        }
    }

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="NetInterface"/> class.
    /// </summary>
    ~NetInterface()
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
                lock (this.NetTerminal.SyncObject)
                {
                    this.Clear();
                    this.NetTerminal.RemoveInternal(this);
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
