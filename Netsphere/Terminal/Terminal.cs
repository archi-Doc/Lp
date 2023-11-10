// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Arc.Crypto;
using LP.T3CS;
using Netsphere.NetStats;

namespace Netsphere;

public class Terminal : UnitBase, IUnitExecutable
{
    public delegate Task InvokeServerDelegate(ServerTerminal terminal);

    internal record struct RawSend
    {
        public RawSend(IPEndPoint endPoint, ByteArrayPool.MemoryOwner owner)
        {
            this.Endpoint = endPoint;
            this.SendOwner = owner.IncrementAndShare();
        }

        public void Clear()
        {
            this.SendOwner = this.SendOwner.Return();
        }

        public IPEndPoint Endpoint { get; }

        public ByteArrayPool.MemoryOwner SendOwner { get; private set; }
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

    /// <summary>
    /// Create unmanaged (without public key) NetTerminal instance.
    /// </summary>
    /// <param name="nodeAddress">NodeAddress.</param>
    /// <returns>NetTerminal.</returns>
    public ClientTerminal Create(NodeAddress nodeAddress)
    {
        var terminal = new ClientTerminal(this, nodeAddress);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    /// <summary>
    /// Create unmanaged (without public key) NetTerminal instance.
    /// </summary>
    /// <param name="address">DualAddress.</param>
    /// <returns>NetTerminal.</returns>
    public ClientTerminal? TryCreate(DualAddress address)
    {
        IPEndPoint? endPoint = default;
        if (this.statsData.MyIpv6Address.AddressState == MyAddress.State.Fixed ||
            this.statsData.MyIpv6Address.AddressState == MyAddress.State.Changed)
        {// Ipv6 supported
            endPoint = address.TryCreateIpv4();
            endPoint ??= address.TryCreateIpv6();
        }
        else
        {// Ipv4
            endPoint = address.TryCreateIpv4();
        }

        if (endPoint is null)
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
    /// Create managed (with public key) NetTerminal instance.
    /// </summary>
    /// <param name="node">NetNode.</param>
    /// <returns>NetTerminal.</returns>
    public ClientTerminal Create(NetNode node)
    {
        var terminal = new ClientTerminal(this, node);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    /// <summary>
    /// Create managed (with public key) NetTerminal instance and create encrypted connection.
    /// </summary>
    /// <param name="node">NodeInformation.</param>
    /// <returns>NetTerminal.</returns>
    public async Task<ClientTerminal?> CreateAndEncrypt(NetNode node)
    {
        var terminal = new ClientTerminal(this, node);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        if (await terminal.EncryptConnectionAsync().ConfigureAwait(false) != NetResult.Success)
        {
            terminal.Dispose();
            return null;
        }

        return terminal;
    }

    /// <summary>
    /// Create managed (with public key) and encrypted NetTerminal instance.
    /// </summary>
    /// <param name="nodeInformation">NodeInformation.</param>
    /// <param name="gene">gene.</param>
    /// <returns>NetTerminal.</returns>
    public ServerTerminal Create(NodeInformation nodeInformation, ulong gene)
    {
        var terminal = new ServerTerminal(this, nodeInformation, gene);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    public void TryRemove(NetTerminal netTerminal)
    {
        lock (this.terminals)
        {
            this.terminals.Remove(netTerminal);
        }
    }

    public Terminal(UnitContext context, UnitLogger unitLogger, NetBase netBase, StatsData statsData)
        : base(context)
    {
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;
        this.NetSocketIpv4 = new(this);
        this.NetSocketIpv6 = new(this);
        this.statsData = statsData;
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.Core = new ThreadCoreGroup(message.ParentCore);

        if (this.Port == 0)
        {
            this.Port = this.NetBase.NetsphereOptions.Port;
        }

        this.NetSocketIpv4.Start(this.Core, this.Port, false);
        this.NetSocketIpv6.Start(this.Core, this.Port, true);
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
            try
            {
                this.Send(rawSend.SendOwner.Memory.Span, rawSend.Endpoint);
            }
            catch
            {
            }

            rawSend.Clear();

            if (--rawCapacity <= 0)
            {
                break;
            }
        }

        NetTerminal[] array;
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
    {
        var packetArray = arrayOwner.ByteArray;
        var position = 0;
        var remaining = packetSize;

        while (remaining >= PacketService.HeaderSize)
        {
            PacketHeader header;
            fixed (byte* pb = packetArray)
            {
                header = *(PacketHeader*)(pb + position);
            }

            var dataSize = header.DataSize;
            if (remaining < (PacketService.HeaderSize + dataSize))
            {// Invalid DataSize
                return;
            }

            if (header.Engagement != 0)
            {
            }

            position += PacketService.HeaderSize;
            var memoryOwner = arrayOwner.ToMemoryOwner(position, dataSize);
            this.ProcessReceiveCore(memoryOwner, endPoint, ref header, currentMics);
            position += dataSize;
            remaining -= PacketService.HeaderSize + dataSize;
        }
    }

    internal void ProcessReceiveCore(ByteArrayPool.MemoryOwner owner, IPEndPoint endPoint, ref PacketHeader header, long currentMics)
    {
        // this.TerminalLogger?.Information($"{header.Gene.To4Hex()}, {header.Id}");
        if (this.inboundGenes.TryGetValue(header.Gene, out var gene))
        {// NetTerminalGene is found.
            gene.NetInterface.ProcessReceive(owner, endPoint, ref header, currentMics, gene);
        }
        else
        {
            this.ProcessUnmanagedRecv(owner, endPoint, ref header);
        }
    }

    internal void ProcessUnmanagedRecv(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {
        if (header.Id == PacketId.Data)
        {
            if (!PacketService.GetData(ref header, ref owner))
            {// Data packet to other packets (e.g Punch, Encrypt).
                // this.TerminalLogger?.Error($"GetData error: {header.Gene.To4Hex()}");
                return;
            }
        }

        if (header.Id == PacketId.Punch)
        {
            this.ProcessUnmanagedRecv_Punch(owner, endpoint, ref header);
        }
        else if (header.Id == PacketId.Encrypt && this.NetBase.EnableServer)
        {
            this.ProcessUnmanagedRecv_Encrypt(owner, endpoint, ref header);
        }
        else if (header.Id == PacketId.Ping)
        {
            this.ProcessUnmanagedRecv_Ping(owner, endpoint, ref header);
        }
        else if (header.Id == PacketId.GetNodeInformation)
        {
            this.ProcessUnmanagedRecv_GetNodeInformation(owner, endpoint, ref header);
        }
        else
        {// Not supported
            // this.TerminalLogger?.Error($"Unhandled: {header.Gene.To4Hex()} - {header.Id}");
        }
    }

    internal void ProcessUnmanagedRecv_Punch(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(owner.Memory.Span, out var punch))
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
                this.AddRawSend(punch.NextEndpoint, sendOwner);
            }
        }
        else
        {
            var response = new PacketPunchResponse();
            response.UtcMics = Mics.GetUtcNow();
            var secondGene = GenePool.NextGene(header.Gene);
            // this.TerminalLogger?.Information($"Punch Response: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");
            if (punch.NextEndpoint == null)
            {
                response.Endpoint = endpoint;

                PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var sendOwner);
                this.AddRawSend(response.Endpoint, sendOwner);
            }
            else
            {
                response.Endpoint = punch.NextEndpoint;

                this.SendRawAck(endpoint, header.Gene);
                header.Gene = secondGene;
                PacketService.CreatePacket(ref header, response, response.PacketId, out var sendOwner);
                this.AddRawSend(response.Endpoint, sendOwner);
            }
        }
    }

    internal void ProcessUnmanagedRecv_Encrypt(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketEncrypt>(owner.Memory.Span, out var packet))
        {
            return;
        }

        if (!packet.Node.Validate())
        {
            packet.Node = packet.Node.WithIpEndPoint(endpoint);

            var response = new PacketEncryptResponse();
            response.Salt2 = RandomVault.Crypto.NextUInt64();
            response.SaltA2 = RandomVault.Crypto.NextUInt64();
            var firstGene = header.Gene;
            var secondGene = GenePool.NextGene(header.Gene);
            PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var sendOwner);

            var terminal = this.Create(packet.Node, firstGene);
            var netInterface = NetInterface<PacketEncryptResponse, PacketEncrypt>.CreateConnect(terminal, firstGene, owner, secondGene, sendOwner);
            sendOwner.Return();

            /*terminal.GenePool.GetSequential();
            terminal.SetSalt(packet.SaltA, response.SaltA2);
            terminal.CreateEmbryo(packet.Salt, response.Salt2);
            terminal.SetReceiverNumber();
            terminal.Add(netInterface); // Delay sending PacketEncryptResponse until the receiver is ready.
            if (this.invokeServerDelegate != null)
            {
                new ThreadCore(ThreadCore.Root, x =>
                {
                    this.invokeServerDelegate(terminal);
                });
            }*/

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
    }

    internal void ProcessUnmanagedRecv_Ping(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPing>(owner.Memory.Span, out var packet))
        {
            return;
        }

        var response = new PacketPingResponse(new(endpoint.Address, (ushort)endpoint.Port), this.NetBase.NodeName);
        var secondGene = GenePool.NextGene(header.Gene);
        // this.TerminalLogger?.Information($"Ping Response: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner);
    }

    internal void ProcessUnmanagedRecv_GetNodeInformation(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {// Checked
        if (!TinyhandSerializer.TryDeserialize<PacketGetNodeInformation>(owner.Memory.Span, out var packet))
        {
            return;
        }

        var response = new PacketGetNodeInformationResponse(default); // tempcode, this.NetStatus.GetMyNodeInformation(this.IsAlternative)
        var secondGene = GenePool.NextGene(header.Gene);
        // this.TerminalLogger?.Information($"GetNodeInformation Response {response.Node.PublicKeyX[0]}: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner);
    }

    internal void Send(ReadOnlySpan<byte> datagram, IPEndPoint endPoint)
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

    internal unsafe void SendRawAck(IPEndPoint endpoint, ulong gene)
    {
        PacketHeader header = default;
        header.Gene = gene;
        header.Id = PacketId.Ack;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.AddRawSend(endpoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddRawSend(IPEndPoint endpoint, ByteArrayPool.MemoryOwner owner)
    {
        this.rawSends.Enqueue(new RawSend(endpoint, owner));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            if (x.State == NetTerminalGeneState.WaitingToReceive ||
                x.State == NetTerminalGeneState.WaitingToSend ||
                x.State == NetTerminalGeneState.WaitingForAck)
            {
                this.inboundGenes.TryAdd(x.Gene, x);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddInbound(NetTerminalGene x)
    {
        if (x.State == NetTerminalGeneState.WaitingToReceive ||
            x.State == NetTerminalGeneState.WaitingToSend ||
            x.State == NetTerminalGeneState.WaitingForAck)
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
            NetTerminal[] array;
            lock (this.terminals)
            {
                array = this.terminals.QueueChain.ToArray();
            }

            List<NetTerminal>? list = null;
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
                x.NetInterface.NetTerminal.Disposed)
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

    internal NetSocket NetSocketIpv4 { get; private set; }

    internal NetSocket NetSocketIpv6 { get; private set; }

    internal UnitLogger UnitLogger { get; private set; }

#pragma warning disable SA1401 // Fields should be private
    internal int SendCapacityPerRound;
#pragma warning restore SA1401 // Fields should be private

    private readonly ILogger<Terminal> logger;
    private readonly StatsData statsData;
    private InvokeServerDelegate? invokeServerDelegate;
    private NetTerminal.GoshujinClass terminals = new();
    private ConcurrentDictionary<ulong, NetTerminalGene> inboundGenes = new();
    private ConcurrentQueue<RawSend> rawSends = new();
    private long lastCleanedMics; // The last mics Terminal.CleanNetTerminal() was called.
}
