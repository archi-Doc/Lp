// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Netsphere;

public class Terminal : UnitBase, IUnitExecutable
{
    public delegate Task InvokeServerDelegate(ServerTerminal terminal);

    internal struct RawSend
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

    public void Dump(ILogger? logger)
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
    /// Create managed (with public key) NetTerminal instance.
    /// </summary>
    /// <param name="nodeInformation">NodeInformation.</param>
    /// <returns>NetTerminal.</returns>
    public ClientTerminal Create(NodeInformation nodeInformation)
    {
        var terminal = new ClientTerminal(this, nodeInformation);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
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

    public Terminal(UnitContext context, UnitLogger logger, NetBase netBase, NetStatus netStatus)
        : base(context)
    {
        this.logger = logger;
        this.NetBase = netBase;
        this.NetStatus = netStatus;
        this.NetSocket = new(logger, this);
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.Core = new ThreadCoreGroup(message.ParentCore);

        if (this.Port == 0)
        {
            this.Port = this.NetBase.NetsphereOptions.Port;
        }

        this.NetSocket.Start(this.Core, this.Port);
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.NetSocket.Stop();
        this.Core?.Dispose();
        this.Core = null;
    }

    public void SetInvokeServerDelegate(InvokeServerDelegate @delegate)
    {
        this.invokeServerDelegate = @delegate;
    }

    public void SetLogger(ILogger logger)
    {
        // this.TerminalLogger = logger;
    }

    public ThreadCoreBase? Core { get; private set; }

    public NetBase NetBase { get; }

    public NetStatus NetStatus { get; }

    public MyStatus MyStatus { get; } = new();

    public bool IsAlternative { get; private set; }

    public int Port { get; set; }

    internal void Initialize(bool isAlternative, NodePrivateKey nodePrivateKey, ECDiffieHellman nodePrivateECDH)
    {
        this.IsAlternative = isAlternative;
        this.NodePrivateKey = nodePrivateKey;
        this.NodePrivateECDH = nodePrivateECDH;
    }

    internal void ProcessSend(long currentMics)
    {
        if ((currentMics - this.lastCleanedMics) > Mics.FromSeconds(1))
        {
            this.lastCleanedMics = currentMics;
            this.CleanNetTerminal(currentMics);
        }

        while (this.rawSends.TryDequeue(out var rawSend))
        {
            try
            {
                this.UnsafeUdpClient?.Send(rawSend.SendOwner.Memory.Span, rawSend.Endpoint);
            }
            catch
            {
            }

            rawSend.Clear();
        }

        NetTerminal[] array;
        lock (this.terminals)
        {
            array = this.terminals.QueueChain.ToArray();
        }

        foreach (var x in array)
        {
            x.ProcessSend(currentMics);
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
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(owner.Memory, out var punch))
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
        if (!TinyhandSerializer.TryDeserialize<PacketEncrypt>(owner.Memory, out var packet))
        {
            return;
        }

        if (packet.NodeInformation != null)
        {
            packet.NodeInformation.SetIPEndPoint(endpoint);

            var response = new PacketEncryptResponse();
            response.Salt2 = LP.Random.Crypto.NextUInt64();
            var firstGene = header.Gene;
            var secondGene = GenePool.NextGene(header.Gene);
            PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var sendOwner);

            var terminal = this.Create(packet.NodeInformation, firstGene);
            var netInterface = NetInterface<PacketEncryptResponse, PacketEncrypt>.CreateConnect(terminal, firstGene, owner, secondGene, sendOwner);
            sendOwner.Return();

            Task.Run(async () =>
            {
                terminal.GenePool.GetSequential();
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
        if (!TinyhandSerializer.TryDeserialize<PacketPing>(owner.Memory, out var packet))
        {
            return;
        }

        // this.logger.TryGet()?.Log($"Ping From: {packet.ToString()}");

        var response = new PacketPingResponse(new(endpoint.Address, (ushort)endpoint.Port, 0), this.NetBase.NodeName);
        var secondGene = GenePool.NextGene(header.Gene);
        // this.TerminalLogger?.Information($"Ping Response: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner);
    }

    internal void ProcessUnmanagedRecv_GetNodeInformation(ByteArrayPool.MemoryOwner owner, IPEndPoint endpoint, ref PacketHeader header)
    {// Checked
        if (!TinyhandSerializer.TryDeserialize<PacketGetNodeInformation>(owner.Memory, out var packet))
        {
            return;
        }

        var response = new PacketGetNodeInformationResponse(this.NetStatus.GetMyNodeInformation(this.IsAlternative));
        var secondGene = GenePool.NextGene(header.Gene);
        // this.TerminalLogger?.Information($"GetNodeInformation Response {response.Node.PublicKeyX[0]}: {header.Gene.To4Hex()} to {secondGene.To4Hex()}");

        PacketService.CreateAckAndPacket(ref header, secondGene, response, response.PacketId, out var packetOwner);
        this.AddRawSend(endpoint, packetOwner);
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

    internal void AddRawSend(IPEndPoint endpoint, ByteArrayPool.MemoryOwner owner)
    {
        this.rawSends.Enqueue(new RawSend(endpoint, owner));
    }

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

    internal void AddInbound(NetTerminalGene x)
    {
        if (x.State == NetTerminalGeneState.WaitingToReceive ||
            x.State == NetTerminalGeneState.WaitingToSend ||
            x.State == NetTerminalGeneState.WaitingForAck)
        {
            this.inboundGenes.TryAdd(x.Gene, x);
        }
    }

    internal void RemoveInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            this.inboundGenes.TryRemove(x.Gene, out _);
        }
    }

    internal void RemoveInbound(NetTerminalGene x)
    {
        this.inboundGenes.TryRemove(x.Gene, out _);
    }

    internal bool TryGetInbound(ulong gene, [MaybeNullWhen(false)] out NetTerminalGene netTerminalGene) => this.inboundGenes.TryGetValue(gene, out netTerminalGene);

    private void CleanNetTerminal(long currentMics)
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
        }
    }

    // internal ILogger? TerminalLogger { get; private set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal ECDiffieHellman NodePrivateECDH { get; private set; } = default!;

    internal NetSocket NetSocket { get; private set; }

    internal UdpClient? UnsafeUdpClient => this.NetSocket.UnsafeUdpClient;

    private UnitLogger logger;
    private InvokeServerDelegate? invokeServerDelegate;
    private NetTerminal.GoshujinClass terminals = new();
    private ConcurrentDictionary<ulong, NetTerminalGene> inboundGenes = new();
    private ConcurrentQueue<RawSend> rawSends = new();
    private long lastCleanedMics; // The last mics Terminal.CleanNetTerminal() was called.
}
