// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LP.Net;

public class Terminal
{
    internal struct UnmanagedSend
    {
        public UnmanagedSend(IPEndPoint endPoint, byte[] packet)
        {
            this.Endpoint = endPoint;
            this.Packet = packet;
        }

        public IPEndPoint Endpoint { get; }

        public byte[] Packet { get; }
    }

    public void Dump(ISimpleLogger logger)
    {
        logger.Information($"Terminal: {this.terminals.QueueChain.Count}");
        logger.Information($"Unmanaged sends: {this.unmanagedSends.Count}");
        logger.Information($"Managed genes: {this.inboundGenes.Count}");
    }

    /// <summary>
    /// Create unmanaged (without public key) NetTerminal instance.
    /// </summary>
    /// <param name="nodeAddress">NodeAddress.</param>
    /// <returns>NetTerminal.</returns>
    public NetTerminal Create(NodeAddress nodeAddress)
    {
        var terminal = new NetTerminal(this, nodeAddress);
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
    public NetTerminal Create(NodeInformation nodeInformation)
    {
        var terminal = new NetTerminal(this, nodeInformation);
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
    public NetTerminal Create(NodeInformation nodeInformation, ulong gene)
    {
        var terminal = new NetTerminal(this, nodeInformation, gene);
        lock (this.terminals)
        {
            this.terminals.Add(terminal);
        }

        return terminal;
    }

    public Terminal(Information information, NetStatus netStatus)
    {
        this.Information = information;
        // this.Private = @private;
        this.NetStatus = netStatus;

        Radio.Open<Message.Start>(this.Start);
        Radio.Open<Message.Stop>(this.Stop);

        this.TerminalLogger = new Logger.PriorityLogger();
        this.netSocket = new(this);
    }

    public void Start(Message.Start message)
    {
        this.Core = new ThreadCoreGroup(message.ParentCore);

        if (this.Port == 0)
        {
            this.Port = this.Information.ConsoleOptions.NetsphereOptions.Port;
        }

        if (!this.netSocket.TryStart(this.Core, this.Port))
        {
            message.Abort = true;
            return;
        }
    }

    public void Stop(Message.Stop message)
    {
        this.Core?.Dispose();
        this.Core = null;
    }

    public ThreadCoreBase? Core { get; private set; }

    public Information Information { get; }

    // public Private Private { get; }

    public NetStatus NetStatus { get; }

    public int Port { get; set; }

    internal void Initialize(bool isAlternative, ECDiffieHellman nodePrivateKey)
    {
        this.NodePrivateECDH = nodePrivateKey;
    }

    internal void ProcessSend(UdpClient udp, long currentTicks)
    {
        while (this.unmanagedSends.TryDequeue(out var unregisteredSend))
        {
            udp.Send(unregisteredSend.Packet, unregisteredSend.Endpoint);
        }

        NetTerminal[] array;
        lock (this.terminals)
        {
            array = this.terminals.QueueChain.ToArray();
        }

        foreach (var x in array)
        {
            x.ProcessSend(udp, currentTicks);
        }
    }

    internal unsafe void ProcessReceive(IPEndPoint endPoint, byte[] outerPacket, long currentTicks)
    {
        var position = 0;
        var remaining = outerPacket.Length;

        while (remaining >= PacketService.HeaderSize)
        {
            PacketHeader header;
            fixed (byte* pb = outerPacket)
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
            var data = new Memory<byte>(outerPacket, position, dataSize);
            this.ProcessReceiveCore(endPoint, ref header, data, currentTicks);
            position += dataSize;
            remaining -= PacketService.HeaderSize + dataSize;
        }
    }

    internal void ProcessReceiveCore(IPEndPoint endPoint, ref PacketHeader header, Memory<byte> data, long currentTicks)
    {
        if (this.inboundGenes.TryGetValue(header.Gene, out var gene))
        {// NetTerminalGene is found.
            gene.NetTerminal.ProcessReceive(endPoint, ref header, data, currentTicks, gene);
        }
        else
        {
            this.ProcessUnmanagedRecv(endPoint, ref header, data);
        }
    }

    internal void ProcessUnmanagedRecv(IPEndPoint endpoint, ref PacketHeader header, Memory<byte> data)
    {
        if (header.Id == PacketId.Punch)
        {// Punch
            this.ProcessUnmanagedRecv_Punch(endpoint, ref header, data);
        }
        else if (header.Id == PacketId.Encrypt)
        {
            this.ProcessUnmanagedRecv_Encrypt(endpoint, ref header, data);
        }
        else if (header.Id == PacketId.Ping)
        {
            this.ProcessUnmanagedRecv_Ping(endpoint, ref header, data);
        }
        else
        {// Not supported
        }
    }

    internal void ProcessUnmanagedRecv_Punch(IPEndPoint endpoint, ref PacketHeader header, Memory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(data, out var punch))
        {
            return;
        }

        TimeCorrection.AddCorrection(punch.UtcTicks);

        var r = new PacketPunchResponse();
        if (punch.NextEndpoint != null)
        {
            r.Endpoint = punch.NextEndpoint;
        }
        else
        {
            r.Endpoint = endpoint;
        }

        r.UtcTicks = Ticks.GetUtcNow();

        header.Gene = GenePool.GetSecond(header.Gene);
        var p = PacketService.CreatePacket(ref header, r);

        this.unmanagedSends.Enqueue(new UnmanagedSend(endpoint, p));
    }

    internal void ProcessUnmanagedRecv_Encrypt(IPEndPoint endpoint, ref PacketHeader header, Memory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketEncrypt>(data, out var packet))
        {
            return;
        }

        if (packet.NodeInformation != null)
        {
            // Logger.Default.Information($"Recv_Encrypt: {header.Gene.ToString()}");
            packet.NodeInformation.SetIPEndPoint(endpoint);

            var terminal = this.Create(packet.NodeInformation, header.Gene);
            terminal.GenePool.GetGene(); // Dispose the first gene.
            terminal.SendPacket(new PacketEncrypt());
            terminal.CreateEmbryo(packet.Salt);
        }
    }

    internal void ProcessUnmanagedRecv_Ping(IPEndPoint endpoint, ref PacketHeader header, Memory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPing>(data, out var packet))
        {
            return;
        }

        Logger.Default.Information($"Ping received from: {packet.ToString()}");

        packet.NodeAddress = new(endpoint.Address, (ushort)endpoint.Port, header.Engagement);
        packet.Text = this.Information.NodeName;
        header.Gene = GenePool.GetSecond(header.Gene);
        var p = PacketService.CreatePacket(ref header, packet);
        this.unmanagedSends.Enqueue(new UnmanagedSend(endpoint, p));
    }

    internal void AddInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            if (x.State == NetTerminalGeneState.WaitingToReceive)
            {
                this.inboundGenes.TryAdd(x.Gene, x);
            }
        }
    }

    internal void RemoveInbound(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            this.inboundGenes.TryRemove(x.Gene, out _);
        }
    }

    internal bool TryGetInbound(ulong gene, [MaybeNullWhen(false)] out NetTerminalGene netTerminalGene) => this.inboundGenes.TryGetValue(gene, out netTerminalGene);

    public NetTerminal.GoshujinClass NetTerminals => this.terminals;

    internal ISimpleLogger? TerminalLogger { get; private set; }

    internal ECDiffieHellman NodePrivateECDH { get; private set; } = default!;

    private NetSocket netSocket;

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> inboundGenes = new();

    private ConcurrentQueue<UnmanagedSend> unmanagedSends = new();
}
