﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Netsphere.Misc;
using Netsphere.Responder;
using Netsphere.Stats;

namespace Netsphere;

public class Terminal : UnitBase, IUnitExecutable
{
    public delegate Task InvokeServerDelegate(ServerTerminal terminal);

    internal readonly record struct RawSend
    {// nspi
        public RawSend(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeMoved)
        {
            this.Endpoint = endPoint;
            this.SendOwner = toBeMoved;
        }

        public readonly IPEndPoint Endpoint;

        public readonly ByteArrayPool.MemoryOwner SendOwner;
    }

    public void Dump(ILog? logger)
    {
        if (logger != null)
        {
            logger.Log($"Terminal: {this.terminals.QueueChain.Count}");
            logger.Log($"Raw sends: {this.rawSends.Count}");
            logger.Log($"Inbound genes: {this.inboundGenes.Count}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCreateEndPoint(in NetAddress address, out NetEndPoint endPoint)
    {
        endPoint = default;
        if (this.statsData.MyIpv6Address.AddressState == MyAddress.State.Fixed ||
            this.statsData.MyIpv6Address.AddressState == MyAddress.State.Changed)
        {// Ipv6 supported
            /*address.TryCreateIpv4(ref endPoint);
            if (endPoint.IsValid)
            {
                return true;
            }*/

            return address.TryCreateIpv6(ref endPoint);
        }
        else
        {// Ipv4
            return address.TryCreateIpv4(ref endPoint);
        }
    }

    /// <summary>
    /// Create unmanaged (without public key) NetTerminalObsolete instance.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <returns>NetTerminalObsolete.</returns>
    public ClientTerminal? TryCreate(NetAddress address)
    {
        this.TryCreateEndPoint(in address, out var endPoint);
        if (!endPoint.IsValid)
        {
            return null;
        }

        var terminal = new ClientTerminal(this, endPoint);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    /// <summary>
    /// Create managed (with public key) NetTerminalObsolete instance.
    /// </summary>
    /// <param name="node">NetNode.</param>
    /// <returns>NetTerminalObsolete.</returns>
    public ClientTerminal? TryCreate(NetNode node)
    {
        this.TryCreateEndPoint(node.Address, out var endPoint);
        if (!endPoint.IsValid)
        {
            return null;
        }

        var terminal = new ClientTerminal(this, endPoint, node);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    public ServerTerminal? TryCreate(NetEndPoint endPoint, NetNode node, ulong gene)
    {
        if (!endPoint.IsValid)
        {
            return default;
        }

        var terminal = new ServerTerminal(this, endPoint, node, gene);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    /// <summary>
    /// Create managed (with public key) NetTerminalObsolete instance and create encrypted connection.
    /// </summary>
    /// <param name="node">NodeInformation.</param>
    /// <returns>NetTerminalObsolete.</returns>
    public async Task<ClientTerminal?> CreateAndEncrypt(NetNode node)
    {
        var terminal = this.TryCreate(node);
        if (terminal is null)
        {
            return null;
        }

        if (await terminal.EncryptConnectionAsync().ConfigureAwait(false) != NetResult.Success)
        {
            terminal.Dispose();
            return null;
        }

        return terminal;
    }

    public void TryRemove(NetTerminalObsolete netTerminal)
    {
        lock (this.terminals)
        {
            this.terminals.Remove(netTerminal);
        }
    }

    public Terminal(UnitContext context, UnitLogger unitLogger, NetBase netBase, NetStats statsData)
        : base(context)
    {
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;
        this.NetSocketIpv4 = new(this.ProcessSend, this.ProcessReceive);
        this.NetSocketIpv6 = new(this.ProcessSend, this.ProcessReceive);
        this.statsData = statsData;
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.Core = new ThreadCoreGroup(message.ParentCore);

        if (this.Port == 0)
        {
            this.Port = this.NetBase.NetsphereOptions.Port;
        }

        if (this.IsAlternative)
        {
            this.Port = 50000; // tempcode
        }
        else
        {
            this.Port = 49999; // tempcode
        }

        if (!this.NetSocketIpv4.Start(this.Core, this.Port, false))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {this.Port}.");
            throw new PanicException();
        }

        if (!this.NetSocketIpv6.Start(this.Core, this.Port, true))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {this.Port}.");
            throw new PanicException();
        }
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.NetSocketIpv4.Stop();
        this.NetSocketIpv6.Stop();
        this.Core?.Dispose();
        this.Core = null;
    }

    public void SetInvokeServerDelegate(InvokeServerDelegate @delegate)
    {
        this.invokeServerDelegate = @delegate;
    }

    public ThreadCoreBase? Core { get; private set; }

    public NetBase NetBase { get; }

    public bool IsAlternative { get; private set; }

    public int Port { get; set; }

    internal void Initialize(bool isAlternative, NodePrivateKey nodePrivateKey)
    {
        this.IsAlternative = isAlternative;
        this.NodePrivateKey = nodePrivateKey;
    }

    internal void ProcessSend(long currentMics)
    {
        var rawCapacity = NetConstants.SendCapacityPerRound / 2;
        while (this.rawSends.TryDequeue(out var rawSend))
        {
            this.TrySend(rawSend.SendOwner.Memory.Span, rawSend.Endpoint);
            rawSend.SendOwner.Return();

            if (--rawCapacity <= 0)
            {
                break;
            }
        }

        NetTerminalObsolete[] array;
        lock (this.terminals)
        {
            array = this.terminals.QueueChain.ToArray();
        }

        this.SendCapacityPerRound = (NetConstants.SendCapacityPerRound / 2) + rawCapacity;
        foreach (var x in array)
        {
            x.ProcessSend(currentMics);
        }

        if ((currentMics - this.lastCleanedMics) > Mics.FromSeconds(1))
        {
            this.lastCleanedMics = currentMics;
            this.CleanNetsphere(currentMics);
        }
    }

    internal unsafe void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.Owner arrayOwner, int packetSize, long currentMics)
    {// nspi
        var packetArray = arrayOwner.ByteArray;
        var position = 0;
        var remaining = packetSize;

        while (remaining >= PacketService.HeaderSizeObsolete)
        {
            PacketHeaderObsolete header;
            fixed (byte* pb = packetArray)
            {
                header = *(PacketHeaderObsolete*)(pb + position);
            }

            var dataSize = header.DataSize;
            if (remaining < (PacketService.HeaderSizeObsolete + dataSize))
            {// Invalid DataSize
                return;
            }

            if (header.Engagement != 0)
            {// Not implemented
            }

            position += PacketService.HeaderSizeObsolete;
            var memoryOwner = arrayOwner.ToMemoryOwner(position, dataSize);
            this.ProcessReceiveCore(memoryOwner, endPoint, ref header, currentMics);
            position += dataSize;
            remaining -= PacketService.HeaderSizeObsolete + dataSize;
        }
    }

    internal void ProcessReceiveCore(ByteArrayPool.MemoryOwner owner, IPEndPoint endPoint, ref PacketHeaderObsolete header, long currentMics)
    {// nspi
        // this.TerminalLogger?.Information($"{header.Gene.To4Hex()}, {header.Id}");
        if (this.inboundGenes.TryGetValue(header.Gene, out var gene))
        {// NetTerminalGene is found.
            gene.NetInterface.ProcessReceive(owner, endPoint, ref header, currentMics, gene);
        }
        else
        {// nspi
            this.ProcessUnmanagedRecv(owner, endPoint, ref header);
        }
    }

    internal void ProcessUnmanagedRecv(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeaderObsolete header)
    {// nspi
        if (header.Id == PacketIdObsolete.Data)
        {
            if (!PacketService.GetData(ref header, ref owner))
            {// Data packet to other packets (e.g Punch, Encrypt).
                // this.TerminalLogger?.Error($"GetData error: {header.Gene.To4Hex()}");
                return;
            }
        }

        if (header.Id == PacketIdObsolete.Encrypt && this.NetBase.EnableServer)
        {
            this.ProcessUnmanagedRecv_Encrypt(owner, endpoint, ref header);
        }
        else if (this.NetBase.NetsphereOptions.EnableEssential)
        {// Essential function
            if (header.Id == PacketIdObsolete.Punch)
            {
                this.ProcessUnmanagedRecv_Punch(owner, endpoint, ref header);
            }
            else if (header.Id == PacketIdObsolete.Ping)
            {
                this.ProcessUnmanagedRecv_Ping(owner, endpoint, ref header);
            }
            else if (header.Id == PacketIdObsolete.GetNodeInformation)
            {
                this.ProcessUnmanagedRecv_GetNodeInformation(owner, endpoint, ref header);
            }
        }

        // Not supported
        // this.TerminalLogger?.Error($"Unhandled: {header.Gene.To4Hex()} - {header.Id}");
    }

    internal void ProcessUnmanagedRecv_Punch(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeaderObsolete header)
    {// nspi
        if (!TinyhandSerializer.TryDeserialize<PacketPunchObsolete>(owner.Memory.Span, out var punch))
        {
            return;
        }

        TimeCorrection.AddCorrection(punch.UtcMics);

        if (punch.Relay)
        {// Relay punch packet
            if (punch.NextEndpoint != null)
            {
                punch.UtcMics = Mics.GetUtcNow();
                punch.Relay = false;
                punch.NextEndpoint = endpoint;

                var secondGene = GenePool.NextGene(header.Gene);
                // this.TerminalLogger?.Information($"Punch Relay: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

                this.SendRawAck(endpoint, header.Gene);
                PacketService.CreatePacket(ref header, punch, punch.PacketId, out var sendOwner);
                this.AddRawSend(punch.NextEndpoint, sendOwner); // nspi
            }
        }
        else
        {
            var response = new PacketPunchResponseObsolete();
            response.UtcMics = Mics.GetUtcNow();
            var secondGene = GenePool.NextGene(header.Gene);
            this.logger.TryGet()?.Log($"Punch Response: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");
            if (punch.NextEndpoint == null)
            {
                response.Endpoint = endpoint;

                PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var sendOwner);
                this.AddRawSend(response.Endpoint, sendOwner); // nspi
            }
            else
            {
                response.Endpoint = punch.NextEndpoint;

                this.SendRawAck(endpoint, header.Gene);
                header.Gene = secondGene;
                PacketService.CreatePacket(ref header, response, response.PacketId, out var sendOwner);
                this.AddRawSend(response.Endpoint, sendOwner); // nspi
            }
        }
    }

    internal void ProcessUnmanagedRecv_Encrypt(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeaderObsolete header)
    { // nspi
        if (!TinyhandSerializer.TryDeserialize<PacketEncryptObsolete>(owner.Memory.Span, out var packet))
        {
            return;
        }

        var response = new PacketEncryptResponseObsolete();
        response.Salt2 = RandomVault.Crypto.NextUInt64();
        response.SaltA2 = RandomVault.Crypto.NextUInt64();
        var firstGene = header.Gene;
        var secondGene = GenePool.NextGene(header.Gene);
        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var sendOwner);

        var address = new NetAddress(endpoint.Address, (ushort)endpoint.Port);
        var node = new NetNode(address, packet.PublicKey);
        var endPoint = new NetEndPoint(endpoint, 0);
        var terminal = this.TryCreate(endPoint, node, firstGene);
        if (terminal is null)
        {
            return;
        }

        var netInterface = NetInterface<PacketEncryptResponseObsolete, PacketEncryptObsolete>.CreateConnect(terminal, firstGene, owner, secondGene, sendOwner);

        _ = Task.Run(async () =>
        {
            terminal.GenePool.GetSequential();
            terminal.SetSalt(packet.SaltA, response.SaltA2);
            terminal.CreateEmbryo(packet.Salt, response.Salt2);
            terminal.SetReceiverNumber();
            terminal.Add(netInterface); // Delay sending PacketEncryptResponse until the receiver is ready.
            if (this.invokeServerDelegate != null)
            {
                await this.invokeServerDelegate(terminal).ConfigureAwait(false);
            }
        });
    }

    internal void ProcessUnmanagedRecv_Ping(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeaderObsolete header)
    {// nspi
        if (!TinyhandSerializer.TryDeserialize<PacketPingObsolete>(owner.Memory.Span, out var packet))
        {
            return;
        }

        var response = new PacketPingResponseObsolete(new(endpoint.Address, (ushort)endpoint.Port), this.NetBase.NodeName);
        var secondGene = GenePool.NextGene(header.Gene);
        // this.logger.TryGet()?.Log($"Ping Response: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner); // nspi
    }

    internal void ProcessUnmanagedRecv_GetNodeInformation(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeaderObsolete header)
    {// nspi
        if (!TinyhandSerializer.TryDeserialize<PacketGetNodeInformationObsolete>(owner.Memory.Span, out var packet))
        {
            return;
        }

        if (this.IsAlternative)
        {
            return;
        }

        var response = new PacketGetNodeInformationResponseObsolete(this.statsData.GetMyNetNode());
        var secondGene = GenePool.NextGene(header.Gene);
        // this.TerminalLogger?.Information($"GetNodeInformation Response {response.Node.PublicKeyX[0]}: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner); // nspi
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void TrySend(ReadOnlySpan<byte> datagram, IPEndPoint endPoint)
    {
        try
        {
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                this.NetSocketIpv6.UnsafeUdpClient?.Send(datagram, endPoint);
            }
            else
            {
                this.NetSocketIpv4.UnsafeUdpClient?.Send(datagram, endPoint);
            }
        }
        catch
        {
        }
    }

    internal unsafe void SendRawAck(IPEndPoint endpoint, ulong gene)
    {
        PacketHeaderObsolete header = default;
        header.Gene = gene;
        header.Id = PacketIdObsolete.Ack;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeaderObsolete*)bp = header;
        }

        this.AddRawSend(endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSizeObsolete)); // nspi
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddRawSend(IPEndPoint endpoint, ByteArrayPool.MemoryOwner toBeMoved)
    {// nspi
        this.rawSends.Enqueue(new RawSend(endpoint, toBeMoved));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            if (x.GeneState == NetTerminalGene.State.WaitingToReceive ||
                x.GeneState == NetTerminalGene.State.WaitingToSend ||
                x.GeneState == NetTerminalGene.State.WaitingForAck)
            {
                this.inboundGenes.TryAdd(x.Gene, x);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddInbound(NetTerminalGene x)
    {
        if (x.GeneState == NetTerminalGene.State.WaitingToReceive ||
            x.GeneState == NetTerminalGene.State.WaitingToSend ||
            x.GeneState == NetTerminalGene.State.WaitingForAck)
        {
            this.inboundGenes.TryAdd(x.Gene, x);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void RemoveInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            this.inboundGenes.TryRemove(x.Gene, out _);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void RemoveInbound(NetTerminalGene x)
    {
        this.inboundGenes.TryRemove(x.Gene, out _);
    }

    internal bool TryGetInbound(ulong gene, [MaybeNullWhen(false)] out NetTerminalGene netTerminalGene) => this.inboundGenes.TryGetValue(gene, out netTerminalGene);

    internal void CleanNetsphere()
        => this.CleanNetsphere(Mics.GetSystem());

    private void CleanNetsphere(long currentMics)
    {
        _ = Task.Run(() =>
        {
            NetTerminalObsolete[] array;
            lock (this.terminals)
            {
                array = this.terminals.QueueChain.ToArray();
            }

            List<NetTerminalObsolete>? list = null;
            foreach (var x in array)
            {
                if (x.TryClean(currentMics))
                {
                    list ??= new();
                    list.Add(x);
                }
            }

            if (list != null)
            {
                foreach (var x in list)
                {
                    x.Dispose();
                }

                this.logger.TryGet()?.Log($"Clean netsphere {list.Count} terminals");
            }

            // Clean NetTerminalGene
            var cleanedGenes = 0;
            var genes = this.inboundGenes.Values;
            foreach (var x in genes)
            {
                if (x.NetInterface.Disposed ||
                x.NetInterface.NetTerminalObsolete.Disposed)
                {
                    x.Clear();
                    cleanedGenes++;
                }
            }

            if (cleanedGenes > 0)
            {
                this.logger.TryGet()?.Log($"Clean netsphere {cleanedGenes} net terminal genes");
            }
        });
    }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal NetSocketObsolete NetSocketIpv4 { get; private set; }

    internal NetSocketObsolete NetSocketIpv6 { get; private set; }

    internal UnitLogger UnitLogger { get; private set; }

#pragma warning disable SA1401 // Fields should be private
    internal int SendCapacityPerRound;
#pragma warning restore SA1401 // Fields should be private

    private readonly ILogger<Terminal> logger;
    private readonly NetStats statsData;
    private InvokeServerDelegate? invokeServerDelegate;
    private NetTerminalObsolete.GoshujinClass terminals = new();
    private ConcurrentDictionary<ulong, NetTerminalGene> inboundGenes = new();
    private ConcurrentQueue<RawSend> rawSends = new();
    private long lastCleanedMics; // The last mics Terminal.CleanNetTerminal() was called.
}
