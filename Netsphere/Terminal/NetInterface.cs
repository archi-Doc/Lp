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

/// <summary>
/// Represents a result of network transmission.
/// </summary>
public enum NetResult
{
    Success,
    Timeout,
    Closed,
    NoDataToSend,
    NoNodeInformation,
    NoEncryptedConnection,
    NoSender,
    NoReceiver,
    SerializationError,
    DeserializationError,
    PacketSizeLimit,
    BlockSizeLimit,
    ReserveError,
}

/// <summary>
/// Represents a received data.<br/>
/// <see cref="NetResult.Success"/>: <see cref="NetReceivedData.Received"/> is valid, and it's preferable to call Return() method.<br/>
/// Other: <see cref="NetReceivedData.Received"/> is default (empty).
/// </summary>
public struct NetReceivedData
{
    public NetReceivedData(NetResult result, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner received)
    {
        this.Result = result;
        this.PacketId = packetId;
        this.DataId = dataId;
        this.Received = received;
    }

    public NetReceivedData(NetResult result)
    {
        this.Result = result;
        this.PacketId = PacketId.Invalid;
        this.DataId = 0;
        this.Received = default;
    }

    public void Return() => this.Received.Return();

    public NetResult Result;
    public PacketId PacketId;
    public ulong DataId;
    public ByteArrayPool.MemoryOwner Received;
}

internal class NetInterface<TSend, TReceive> : NetInterface
{
    internal static NetInterface<TSend, TReceive>? CreateData(NetOperation netOperation, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner, bool receive, out NetResult interfaceResult)
    {// Send and Receive(optional) NetTerminalGene.
        if (owner.Memory.Length > BlockService.MaxBlockSize)
        {
            interfaceResult = NetResult.BlockSizeLimit;
            return null;
        }

        var netTerminal = netOperation.NetTerminal;
        interfaceResult = NetResult.Success;
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);
        var sequentialGenes = netOperation.Get2Genes(); // Send gene
        netTerminal.CreateHeader(out var header, sequentialGenes.First);
        if (owner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            PacketService.CreateDataPacket(ref header, packetId, dataId, owner.Memory.Span, out var sendOwner);

            var ntg = new NetTerminalGene(sequentialGenes.First, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);
            sendOwner.Return();

            netTerminal.TerminalLogger?.Information($"RegisterSend2  : {sequentialGenes.First.To4Hex()}");
        }
        else
        {
            netInterface.SendGenes = CreateSendGenes(netOperation, netInterface, sequentialGenes.First, owner, dataId);
        }

        // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(sequentialGenes.Second, netInterface);
            netInterface.RecvGenes = new NetTerminalGene[] { ntg, };
            ntg.SetReceive();

            netTerminal.TerminalLogger?.Information($"RegisterReceive2:{sequentialGenes.Second.To4Hex()}");
        }

        netTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetInterface<TSend, TReceive>? CreateValue(NetOperation netOperation, TSend value, PacketId id, bool receive, out NetResult interfaceResult)
    {// Send and Receive(optional) NetTerminalGene.
        NetInterface<TSend, TReceive>? netInterface;
        (ulong First, ulong Second) sequentialGenes;
        interfaceResult = NetResult.Success;

        var netTerminal = netOperation.NetTerminal;
        netTerminal.CreateHeader(out var header, 0); // Set gene in a later code.
        PacketService.CreatePacket(ref header, value, id, out var sendOwner);
        if (sendOwner.Memory.Length <= PacketService.SafeMaxPayloadSize)
        {// Single packet.
            sequentialGenes = netOperation.Get2Genes(); // Send gene
            PacketService.InsertGene(sendOwner.Memory, sequentialGenes.First);

            netInterface = new NetInterface<TSend, TReceive>(netTerminal);
            var ntg = new NetTerminalGene(sequentialGenes.First, netInterface);
            netInterface.SendGenes = new NetTerminalGene[] { ntg, };
            ntg.SetSend(sendOwner);
            sendOwner.Return();

            netTerminal.TerminalLogger?.Information($"RegisterSend   : {sequentialGenes.First.To4Hex()}, {id}");
        }
        else
        {// Packet size limit exceeded.
            sendOwner.Return();
            interfaceResult = NetResult.PacketSizeLimit;
            return null;
        }

        // Receive gene
        if (receive)
        {
            var ntg = new NetTerminalGene(sequentialGenes.Second, netInterface);
            netInterface.RecvGenes = new NetTerminalGene[] { ntg, };
            ntg.SetReceive();

            netTerminal.TerminalLogger?.Information($"RegisterReceive: {sequentialGenes.Second.To4Hex()}");
        }

        netTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetInterface<TSend, TReceive>? CreateReserve(NetOperation netOperation, PacketReserve reserve)
    {// Send and Receive(optional) NetTerminalGene.
        NetInterface<TSend, TReceive>? netInterface;
        (ulong First, ulong Second) sequentialGenes;

        var netTerminal = netOperation.NetTerminal;
        sequentialGenes = netOperation.Get2Genes(); // Send gene

        var response = new PacketReserveResponse();
        netTerminal.CreateHeader(out var header, sequentialGenes.First);
        PacketService.CreatePacket(ref header, response, response.PacketId, out var sendOwner);

        netInterface = new NetInterface<TSend, TReceive>(netTerminal);
        var ntg = new NetTerminalGene(sequentialGenes.First, netInterface);
        netInterface.SendGenes = new NetTerminalGene[] { ntg, };
        ntg.SetSend(sendOwner);
        sendOwner.Return();

        netTerminal.TerminalLogger?.Information($"RegisterSend5  : {sequentialGenes.First.To4Hex()}, {response.PacketId}");

        // Receive gene
        Span<ulong> arraySpan = stackalloc ulong[reserve.NumberOfGenes];
        netOperation.GetGenes(arraySpan);

        var genes = new NetTerminalGene[reserve.NumberOfGenes];
        for (var i = 0; i < reserve.NumberOfGenes; i++)
        {
            var g = new NetTerminalGene(arraySpan[i], netInterface);
            g.SetReceive();
            genes[i] = g;
        }

        netInterface.RecvGenes = genes;

        netTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetInterface<TSend, TReceive> CreateConnect(NetTerminal netTerminal, ulong gene, ByteArrayPool.MemoryOwner receiveOwner, ulong secondGene, ByteArrayPool.MemoryOwner sendOwner)
    {// Only for connection.
        var netInterface = new NetInterface<TSend, TReceive>(netTerminal);

        var recvGene = new NetTerminalGene(gene, netInterface);
        netInterface.RecvGenes = new NetTerminalGene[] { recvGene, };
        recvGene.SetReceive();
        recvGene.Receive(PacketId.Encrypt, receiveOwner, Ticks.GetSystem());

        var sendGene = new NetTerminalGene(secondGene, netInterface);
        netInterface.SendGenes = new NetTerminalGene[] { sendGene, };
        sendGene.SetSend(sendOwner);

        netInterface.NetTerminal.TerminalLogger?.Information($"ConnectTerminal: {gene.To4Hex()} -> {secondGene.To4Hex()}");

        netInterface.NetTerminal.Add(netInterface);
        return netInterface;
    }

    internal static NetTerminalGene[] CreateSendGenes(NetOperation netOperation, NetInterface<TSend, TReceive> netInterface, ulong gene, ByteArrayPool.MemoryOwner owner, ulong dataId)
    {
        ReadOnlySpan<byte> span = owner.Memory.Span;
        var netTerminal = netInterface.NetTerminal;
        var info = PacketService.GetDataSize(owner.Memory.Length);

        Span<ulong> arraySpan = stackalloc ulong[info.NumberOfGenes];
        netOperation.GetGenes(arraySpan);

        var genes = new NetTerminalGene[info.NumberOfGenes];
        for (var i = 0; i < info.NumberOfGenes; i++)
        {
            int size;
            ByteArrayPool.MemoryOwner sendOwner;
            if (i == 0)
            {// First
                size = info.FirstDataSize;

                netTerminal.CreateHeader(out var header, arraySpan[i]);
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

                netTerminal.CreateHeader(out var header, arraySpan[i]);
                PacketService.CreateDataFollowingPacket(ref header, span.Slice(0, size), out sendOwner);
            }

            span = span.Slice(size);

            genes[i] = new(arraySpan[i], netInterface);
            genes[i].SetSend(sendOwner);
            sendOwner.Return();
        }

        return genes;
    }

    internal static NetInterface<TSend, TReceive> CreateReceive(NetOperation netOperation)
    {// Receive
        var netInterface = new NetInterface<TSend, TReceive>(netOperation.NetTerminal);

        (var receiveGene, netInterface.StandbyGene) = netOperation.Get2Genes();
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

    internal bool SetSend(NetOperation netOperation, PacketId packetId, ulong dataId, ByteArrayPool.MemoryOwner owner)
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
        else
        {
            this.SendGenes = CreateSendGenes(netOperation, this, gene, owner, dataId);
        }

        return false;
    }

    internal void SetReserve(NetOperation netOperation, PacketReserve reserve)
    {
        if (this.RecvGenes == null || this.RecvGenes.Length < 1)
        {
            return;
        }

        this.Clear();
        Span<ulong> arraySpan = stackalloc ulong[reserve.NumberOfGenes];
        netOperation.GetGenes(arraySpan);

        var genes = new NetTerminalGene[reserve.NumberOfGenes];
        for (var i = 0; i < reserve.NumberOfGenes; i++)
        {
            var g = new NetTerminalGene(arraySpan[i], this);
            g.SetReceive();
            genes[i] = g;
        }

        this.TerminalLogger?.Information($"SetReserve: {string.Join(", ", genes.Select(x => x.Gene.To4Hex()))}");

        this.RecvGenes = genes;
    }
}

public class NetInterface : IDisposable
{
    public const int IntervalInMilliseconds = 2;

    protected private NetInterface(NetTerminal netTerminal)
    {
        this.Terminal = netTerminal.Terminal;
        this.NetTerminal = netTerminal;
    }

    public Terminal Terminal { get; }

    public NetTerminal NetTerminal { get; }

    /// <summary>
    /// Wait until the data transmission is completed.
    /// </summary>
    /// <returns><see cref="NetResult"/>.</returns>
    public async Task<NetResult> WaitForSendCompletionAsync()
    {// Checked
        this.NetTerminal.ResetLastResponseTicks();

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Ticks.GetSystem() >= (this.NetTerminal.LastResponseTicks + this.NetTerminal.MaximumResponseTicks))
            {
                this.TerminalLogger?.Information($"Send timeout.");
                return NetResult.Timeout;
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.SendGenes == null)
                {
                    return NetResult.NoDataToSend;
                }

                foreach (var x in this.SendGenes)
                {
                    if (!x.IsSendComplete)
                    {
                        goto WaitForSendCompletionWait;
                    }
                }

                return NetResult.Success;
            }

WaitForSendCompletionWait:
            try
            {
                var ct = this.Terminal.Core?.CancellationToken ?? CancellationToken.None;
                await Task.Delay(NetInterface.IntervalInMilliseconds, ct).ConfigureAwait(false);
            }
            catch
            {
                return NetResult.Closed;
            }
        }

        return NetResult.Closed;
    }

    /// <summary>
    /// Wait until data reception is complete.
    /// </summary>
    /// <returns><see cref="NetReceivedData"/>.</returns>
    public async Task<NetReceivedData> ReceiveAsync()
    {// Checked
        ByteArrayPool.MemoryOwner data = default;
        this.NetTerminal.ResetLastResponseTicks();

        while (this.Terminal.Core?.IsTerminated == false && this.NetTerminal.IsClosed == false)
        {
            if (Ticks.GetSystem() >= (this.NetTerminal.LastResponseTicks + this.NetTerminal.MaximumResponseTicks))
            {
                this.TerminalLogger?.Information($"Receive timeout.");
                return new NetReceivedData(NetResult.Timeout);
            }

            lock (this.NetTerminal.SyncObject)
            {
                if (this.ReceivedGeneToData(out var packetId, out var dataId, ref data))
                {
                    return new NetReceivedData(NetResult.Success, packetId, dataId, data);
                }
            }

            try
            {
                var ct = this.Terminal.Core?.CancellationToken ?? CancellationToken.None;
                await Task.Delay(NetInterface.IntervalInMilliseconds, ct).ConfigureAwait(false);
            }
            catch
            {
                return new NetReceivedData(NetResult.Closed);
            }
        }

        return new NetReceivedData(NetResult.Closed);
    }

    internal ISimpleLogger? TerminalLogger => this.Terminal.TerminalLogger;

    protected bool ReceivedGeneToData(out PacketId packetId, out ulong dataId, ref ByteArrayPool.MemoryOwner dataMemory)
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
            dataMemory = this.RecvGenes[0].Owner.IncrementAndShare();
            if (packetId == PacketId.Data)
            {
                var data = PacketService.GetData(dataMemory);
                dataId = data.DataId;
                packetId = data.PacketId;
                dataMemory = data.DataMemory;
            }

            return true;
        }

        // Multiple genes (PacketData)

        // Check
        while (true)
        {
            if (this.RecvCompleteIndex == this.RecvGenes.Length)
            {// Complete
                break;
            }
            else if (!this.RecvGenes[this.RecvCompleteIndex].IsReceiveComplete)
            {// Not received
                return false;
            }
            else
            {
                this.RecvCompleteIndex++;
            }
        }

        // Complete
        var totalSize = 0;
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            if (this.RecvGenes[i].ReceivedId == PacketId.Data)
            {// Data
                totalSize += this.RecvGenes[i].Owner.Memory.Length - PacketService.DataHeaderSize;
            }
            else
            {// DataFollowing
                totalSize += this.RecvGenes[i].Owner.Memory.Length - PacketService.DataFollowingHeaderSize;
            }
        }

        if (totalSize > BlockService.MaxBlockSize)
        {
            return false;
        }

        dataMemory = BlockPool.Rent(totalSize).ToMemoryOwner();
        var memory = dataMemory.Memory;
        for (var i = 0; i < this.RecvGenes.Length; i++)
        {
            if (this.RecvGenes[i].ReceivedId == PacketId.Data)
            {// Data
                var data = PacketService.GetData(this.RecvGenes[i].Owner);
                dataId = data.DataId;
                data.DataMemory.Memory.CopyTo(memory);
                memory = memory.Slice(data.DataMemory.Memory.Length);
            }
            else
            {// DataFollowing
                var data = PacketService.GetDataFollowing(this.RecvGenes[i].Owner.Memory);
                data.CopyTo(memory);
                memory = memory.Slice(data.Length);
            }
        }

        packetId = this.RecvGenes[0].ReceivedId;
        return true;
    }

#pragma warning disable SA1401 // Fields should be private
    internal NetTerminalGene[]? SendGenes;
    internal NetTerminalGene[]? RecvGenes;
    internal int SendCompleteIndex;
    internal int RecvCompleteIndex;
    internal ulong StandbyGene;
    internal long DisposedTicks;
#pragma warning restore SA1401 // Fields should be private

    internal void ProcessSend(UdpClient udp, long currentTicks, ref int sendCapacity)
    {// lock (this.NetTerminal.SyncObject)
        if (this.SendGenes != null)
        {
            foreach (var x in this.SendGenes)
            {
                if (sendCapacity == 0)
                {
                    return;
                }

                if (x.State == NetTerminalGeneState.WaitingToSend)
                {
                    if (x.Send(udp))
                    {
                        this.TerminalLogger?.Information($"Udp Sent       : {x.ToString()}");

                        sendCapacity--;
                        x.SendCount = 1;
                        x.SentTicks = currentTicks;
                    }
                }
                else if (x.State == NetTerminalGeneState.WaitingForAck)
                {
                    if (x.SendCount > NetConstants.SendCountMax)
                    {
                        this.TerminalLogger?.Information($"InternalClose (SentCount)");
                        this.NetTerminal.InternalClose();
                    }
                    else if ((currentTicks - x.SentTicks) > Ticks.FromMilliseconds(NetConstants.ResendWaitMilliseconds))
                    {
                        if (x.Send(udp))
                        {
                            this.TerminalLogger?.Information($"Udp Resent     : {x.ToString()}");
                            sendCapacity--;
                            x.SendCount++;
                            x.SentTicks = currentTicks;
                            this.NetTerminal.IncrementResendCount();
                        }
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
        ByteArrayPool.Owner? rentArray = null;
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

            this.NetTerminal.SetLastResponseTicks(currentTicks);
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
                if (gene.Receive(header.Id, owner, currentTicks))
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
            {// Delay the disposal.
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
