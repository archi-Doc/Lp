// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
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
    ReserveError,
}

public struct NetInterfaceReceivedData
{
    public NetInterfaceReceivedData(NetInterfaceResult result, PacketId packetId, ulong dataId, ReadOnlyMemory<byte> received)
    {
        this.Result = result;
        this.PacketId = packetId;
        this.DataId = dataId;
        this.Received = received;
    }

    public NetInterfaceReceivedData(NetInterfaceResult result)
    {
        this.Result = result;
        this.PacketId = PacketId.Invalid;
        this.DataId = 0;
        this.Received = default;
    }

    public NetInterfaceResult Result;
    public PacketId PacketId;
    public ulong DataId;
    public ReadOnlyMemory<byte> Received;
}

public interface INetInterface<TSend, TReceive> : INetInterface<TSend>
{
    // public NetInterfaceResult Receive(out TReceive? value, int millisecondsToWait = DefaultMillisecondsToWait);
}

public interface INetInterface<TSend> : IDisposable
{
    public const int DefaultMillisecondsToWait = 2000;

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = DefaultMillisecondsToWait);
}

internal class NetInterface<TSend, TReceive> : NetInterface, INetInterface<TSend, TReceive>
{
    internal static NetInterface<TSend, TReceive>? CreateData(NetTerminal netTerminal, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, bool receive, out NetInterfaceResult interfaceResult)
    {// Send and Receive(optional) NetTerminalGene.
        if (owner.Memory.Length > BlockService.MaxBlockSize)
        {
            interfaceResult = NetInterfaceResult.BlockSizeLimit;
            return null;
        }

        interfaceResult = NetInterfaceResult.Success;
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);
        var gene = netTerminal.GenePool.GetGene(); // Send gene
        netTerminal.CreateHeader(out var header, gene);
        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            PacketService.CreateDataPacket(ref header, packetId, dataId, owner.Memory.Span, out var sendOwner);

            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);
            sendOwner.Return();

            netTerminal.TerminalLogger?.Information($"RegisterSend2  : {gene.To4Hex()}");
        }
        else
        {
            netInterface.SendGenes = CreateSendGenes(netInterface, gene, owner, dataId);
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

    internal static NetInterface<TSend, TReceive>? CreateValue(NetTerminal netTerminal, TSend value, PacketId id, bool receive, out NetInterfaceResult interfaceResult)
    {// Send and Receive(optional) NetTerminalGene.
        NetInterface<TSend, TReceive>? netInterface;
        ulong gene;
        interfaceResult = NetInterfaceResult.Success;

        netTerminal.CreateHeader(out var header, 0); // Set gene in a later code.
        PacketService.CreatePacket(ref header, value, id, out var sendOwner);
        if (sendOwner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            gene = netTerminal.GenePool.GetGene(); // Send gene
            PacketService.InsertGene(sendOwner.Memory, gene);

            netInterface = new NetInterface<TSend, TReceive>(netTerminal);
            var ntg = new NetTerminalGene(gene, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);

            netTerminal.TerminalLogger?.Information($"RegisterSend   : {gene.To4Hex()}, {id}");
        }
        else
        {// Packet size limit exceeded.
            sendOwner.Return();
            interfaceResult = NetInterfaceResult.PacketSizeLimit;
            return null;
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

    internal static NetInterface<TSend, TReceive> CreateConnect(NetTerminal netTerminal, ulong gene, FixedArrayPool.MemoryOwner receiveOwner, ulong secondGene, FixedArrayPool.MemoryOwner sendOwner)
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

    internal static NetTerminalGene[] CreateSendGenes(NetInterface<TSend, TReceive> netInterface, ulong gene, ByteArrayPool.MemoryOwner owner, ulong dataId)
    {
        ReadOnlySpan<byte> span = owner.Memory.Span;
        var netTerminal = netInterface.NetTerminal;
        var info = PacketService.GetDataSize(owner.Memory.Length);
        var rentArray = netTerminal.RentAndSetGeneArray(gene, info.NumberOfGenes);
        var geneArray = MemoryMarshal.Cast<byte, ulong>(rentArray);

        var genes = new NetTerminalGene[info.NumberOfGenes];
        for (var i = 0; i < info.NumberOfGenes; i++)
        {
            int size;
            FixedArrayPool.MemoryOwner sendOwner;
            if (i == 0)
            {// First
                size = info.FirstDataSize;

                netTerminal.CreateHeader(out var header, geneArray[i]);
                PacketService.CreateDataPacket(ref header, PacketId.Data, dataId, span.Slice(0, size), out sendOwner);
            }
            else
            {
                if (i == (info.NumberOfGenes - 1))
                {// Last
                    size = info.LastDataSize;
                }
                else
                {// Following
                    size = info.FollowingDataSize;
                }

                netTerminal.CreateHeader(out var header, geneArray[i]);
                PacketService.CreateDataFollowingPacket(ref header, span.Slice(0, size), out sendOwner);
            }

            span = span.Slice(size);

            genes[i] = new(geneArray[i], netInterface);
            genes[i].SetSend(sendOwner);
            sendOwner.Return();
        }

        ArrayPool<byte>.Shared.Return(rentArray);

        return genes;
    }

    internal static NetInterface<TSend, TReceive> CreateReceive(NetTerminal netTerminal)
    {// Receive
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);

        var receiveGene = netTerminal.GenePool.GetGene();
        netInterface.StandbyGene = netTerminal.GenePool.GetGene();
        var gene = new NetTerminalGene(receiveGene, netInterface);
        netInterface.RecvGenes = new NetTerminalGene[] { gene, };
        gene.SetReceive();

        netInterface.NetTerminal.TerminalLogger?.Information($"ReceiveInterface: {receiveGene.To4Hex()}");

        netInterface.NetTerminal.Add(netInterface);
        return netInterface;
    }

    protected NetInterface(NetTerminal netTerminal)
    : base(netTerminal)
    {
    }

    /*public NetInterfaceResult Receive(out TReceive? value, int millisecondsToWait = 2000)
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
    }*/

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

    public async Task<NetInterfaceReceivedData> ReceiveDataAsync(int millisecondsToWait = 2000)
    {
        var r = await this.ReceiveAsyncCore(millisecondsToWait).ConfigureAwait(false);
        return r;
    }

    public NetInterfaceResult WaitForSendCompletion(int millisecondsToWait = 2000)
        => this.WaitForSendCompletionCore(millisecondsToWait);

    public Task<NetInterfaceResult> WaitForSendCompletionAsync(int millisecondsToWait = 2000)
        => this.WaitForSendCompletionAsyncCore(millisecondsToWait);

    internal bool SetSend<TValue>(TValue value)
        where TValue : IPacket
    {
        if (this.SendGenes != null)
        {
            return false;
        }

        var gene = this.StandbyGene;
        this.NetTerminal.CreateHeader(out var header, gene);
        PacketService.CreatePacket(ref header, value, value.PacketId, out var sendOwner);
        if (sendOwner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            var ntg = new NetTerminalGene(gene, this);
            this.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);

            this.TerminalLogger?.Information($"RegisterSend3  : {gene.To4Hex()}");
            return true;
        }
        else
        {// Packet size limit exceeded.
            return false;
        }
    }

    internal bool SetSend(PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner)
    {
        if (this.SendGenes != null)
        {
            return false;
        }

        var gene = this.StandbyGene;
        this.NetTerminal.CreateHeader(out var header, gene);
        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            PacketService.CreateDataPacket(ref header, packetId, dataId, owner.Memory.Span, out var sendOwner);
            var ntg = new NetTerminalGene(gene, this);
            this.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);
            sendOwner.Return();

            this.TerminalLogger?.Information($"RegisterSend4  : {gene.To4Hex()}");
            return true;
        }

        return false;
    }

    internal void SetReserve(PacketReserve reserve)
    {
        if (this.RecvGenes == null || this.RecvGenes.Length < 1)
        {
            return;
        }

        var gene = this.RecvGenes[0].Gene;
        this.Clear();

        var rentArray = this.NetTerminal.RentAndSetGeneArray(gene, reserve.NumberOfGenes);
        var geneArray = MemoryMarshal.Cast<byte, ulong>(rentArray);

        var genes = new NetTerminalGene[reserve.NumberOfGenes];
        for (var i = 0; i < reserve.NumberOfGenes; i++)
        {
            var g = new NetTerminalGene(geneArray[i], this);
            g.SetReceive();
            genes[i] = g;
        }

        this.TerminalLogger?.Information($"SetReserve: {string.Join(", ", genes.Select(x => x.Gene.To4Hex()))}");

        ArrayPool<byte>.Shared.Return(rentArray);
        this.RecvGenes = genes;
    }
}

public class NetInterface : IDisposable
{
    public const int IntervalInMilliseconds = 2;

    protected NetInterface(NetTerminal netTerminal)
    {
        this.Terminal = netTerminal.Terminal;
        this.NetTerminal = netTerminal;
    }

    public Terminal Terminal { get; }

    public NetTerminal NetTerminal { get; }

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
                    if (!x.IsSendComplete)
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
                    if (!x.IsSendComplete)
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
                return NetInterfaceResult.Closed;
            }
        }

        return NetInterfaceResult.Closed;
    }

    /*protected NetInterfaceResult ReceiveCore(out PacketId packetId, out ReadOnlyMemory<byte> data, int millisecondsToWait)
    {
        packetId = PacketId.Invalid;
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
                if (this.ReceivedGeneToData(out packetId, ref data))
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
    }*/

    private protected async Task<NetInterfaceReceivedData> ReceiveAsyncCore(int millisecondsToWait)
    {
        ReadOnlyMemory<byte> data = default;
        var end = Stopwatch.GetTimestamp() + (long)(millisecondsToWait * (double)Stopwatch.Frequency / 1000);

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Stopwatch.GetTimestamp() >= end)
            {
                this.TerminalLogger?.Information($"Receive timeout.");
                return new NetInterfaceReceivedData(NetInterfaceResult.Timeout);
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.ReceivedGeneToData(out var packetId, out var dataId, ref data))
                {
                    return new NetInterfaceReceivedData(NetInterfaceResult.Success, packetId, dataId, data);
                }
            }

            try
            {
                var ct = this.Terminal.Core?.CancellationToken ?? CancellationToken.None;
                await Task.Delay(NetInterface.IntervalInMilliseconds, ct).ConfigureAwait(false);
            }
            catch
            {
                return new NetInterfaceReceivedData(NetInterfaceResult.Closed);
            }
        }

        return new NetInterfaceReceivedData(NetInterfaceResult.Closed);
    }

    internal ISimpleLogger? TerminalLogger => this.Terminal.TerminalLogger;

    protected bool ReceivedGeneToData(out PacketId packetId, out ulong dataId, ref ReadOnlyMemory<byte> dataMemory)
    {// lock (this.NetTerminal.SyncObject)
        packetId = PacketId.Invalid;
        dataId = 0;
        if (this.RecvGenes == null)
        {// Empty
            return true;
        }
        else if (this.RecvGenes.Length == 1)
        {// Single gene
            if (!this.RecvGenes[0].IsReceiveComplete)
            {
                return false;
            }

            packetId = this.RecvGenes[0].ReceivedId;
            dataMemory = this.RecvGenes[0].Owner.Memory;
            if (packetId == PacketId.Data)
            {
                var data = PacketService.GetData(dataMemory);
                dataId = data.DataId;
                dataMemory = data.DataMemory;
            }

            return true;
        }

        // Multiple genes (PacketData)
        var total = 0;
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            if (!this.RecvGenes[i].IsReceiveComplete)
            { // Checked later: this.RecvGenes[i].Owner.Memory.Length < PacketService.DataHeaderSize
                return false;
            }
            else
            {
                if (this.RecvGenes[i].ReceivedId == PacketId.Data)
                {// Data
                    total += this.RecvGenes[i].Owner.Memory.Length - PacketService.DataHeaderSize;
                }
                else
                {// DataFollowing
                    total += this.RecvGenes[i].Owner.Memory.Length - PacketService.DataFollowingHeaderSize;
                }
            }
        }

        var buffer = new byte[total]; // temporary
        var mem = buffer.AsMemory();
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            if (this.RecvGenes[i].ReceivedId == PacketId.Data)
            {// Data
                var data = PacketService.GetData(this.RecvGenes[i].Owner.Memory);
                dataId = data.DataId;
                data.DataMemory.CopyTo(mem);
                mem = mem.Slice(data.DataMemory.Length);
            }
            else
            {// DataFollowing
                var data = PacketService.GetDataFollowing(this.RecvGenes[i].Owner.Memory);
                data.CopyTo(mem);
                mem = mem.Slice(data.Length);
            }
        }

        packetId = this.RecvGenes[0].ReceivedId;
        dataMemory = buffer;
        return true;
    }

#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? SendGenes;
    internal NetTerminalGene[]? RecvGenes;
    internal ulong StandbyGene;
    internal long DisposedTicks;
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

    internal unsafe void ProcessSendingAck()
    {
        if (this.RecvGenes == null)
        {
            return;
        }

        PacketHeader header = default;
        int size = 0;
        int maxSize = PacketService.DataPayloadSize - sizeof(ulong);
        FixedArrayPool.Owner? rentArray = null;
        foreach (var x in this.RecvGenes)
        {
            if (x.State == NetTerminalGeneState.SendingAck)
            {
                if (size >= maxSize)
                {
                    PacketService.InsertDataSize(rentArray!.ByteArray, (ushort)(size - PacketService.HeaderSize));
                    this.Terminal.AddRawSend(this.NetTerminal.Endpoint, rentArray!.ToMemoryOwner(0, size));
                    size = 0;
                }

                if (size == 0)
                {
                    this.NetTerminal.CreateHeader(out header, x.Gene);
                    header.Id = PacketId.Ack;

                    rentArray ??= PacketPool.Rent();
                    size += PacketService.HeaderSize;
                    fixed (byte* bp = rentArray.ByteArray)
                    {
                        *(PacketHeader*)bp = header;
                    }
                }
                else
                {
                    fixed (byte* bp = rentArray!.ByteArray)
                    {
                        *(ulong*)(bp + size) = x.Gene;
                    }

                    size += sizeof(ulong);
                }

                x.State = NetTerminalGeneState.ReceiveComplete;
            }
        }

        if (size > 0)
        {
            PacketService.InsertDataSize(rentArray!.ByteArray, (ushort)(size - PacketService.HeaderSize));
            this.Terminal.AddRawSend(this.NetTerminal.Endpoint, rentArray!.ToMemoryOwner(0, size));
            size = 0;
        }

        rentArray?.Return();
    }

    internal void ProcessReceive(FixedArrayPool.MemoryOwner owner, IPEndPoint endPoint, ref PacketHeader header, long currentTicks, NetTerminalGene gene)
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
                this.TerminalLogger?.Information($"Recv Ack 1+{g.Length}, {header.Gene.To4Hex()}");
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
            else if (header.Id == PacketId.Close)
            {
                this.TerminalLogger?.Information($"Close, {header.Gene.To4Hex()}");
                this.NetTerminal.IsClosed = true;
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
        lock (this.NetTerminal.SyncObject)
        {
            if (this.DisposedTicks == 0)
            {
                this.NetTerminal.ActiveToDisposed(this);
                this.DisposedTicks = Ticks.GetSystem();
            }
        }
    }

    internal void DisposeActual()
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
                    Debug.Assert(this.NetTerminal.RemoveInternal(this) != false);
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
