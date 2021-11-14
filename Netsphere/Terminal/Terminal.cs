// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;

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

    public Terminal(Information information, Private @private)
    {
        this.Information = information;
        this.Private = @private;

        Radio.Open<Message.Start>(this.Start);
        Radio.Open<Message.Stop>(this.Stop);

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

    public Private Private { get; }

    public int Port { get; set; }

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

    internal unsafe void ProcessReceive(IPEndPoint endPoint, byte[] packet)
    {
        var position = 0;
        var remaining = packet.Length;

        while (remaining >= PacketService.HeaderSize)
        {
            PacketHeader header;
            fixed (byte* pb = packet)
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
            var data = new Memory<byte>(packet, position, dataSize);
            this.ProcessReceiveCore(endPoint, ref header, data);
            position += dataSize;
            remaining -= PacketService.HeaderSize + dataSize;
        }
    }

    internal void ProcessReceiveCore(IPEndPoint endPoint, ref PacketHeader header, Memory<byte> data)
    {
        if (this.managedGenes.TryGetValue(header.Gene, out var terminalGene) &&
            terminalGene.State != NetTerminalGeneState.Unmanaged &&
            terminalGene.PacketId == header.Id)
        {// NetTerminalGene is found and the state is not unmanaged and packet ids are identical.
            var netTerminal = terminalGene.NetTerminal;
            if (!netTerminal.Endpoint.Equals(endPoint))
            {// Endpoint mismatch.
                return;
            }

            terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, data);

            /*if (!terminalGene.NetTerminal.ProcessRecv(terminalGene, endPoint, ref header, data))
            {
                this.ProcessUnmanagedRecv(endPoint, ref header, data);
            }*/
        }
        else
        {
            this.ProcessUnmanagedRecv(endPoint, ref header, data);
        }
    }

    internal unsafe void ProcessUnmanagedRecv(IPEndPoint endPoint, ref PacketHeader header, Memory<byte> data)
    {
        if (header.Id == PacketId.Punch)
        {// Punch
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
                r.Endpoint = endPoint;
            }

            r.UtcTicks = Ticks.GetUtcNow();

            var p = PacketService.CreatePacket(ref header, r);

            this.unmanagedSends.Enqueue(new UnmanagedSend(endPoint, p));
        }
        else
        {// Not supported
        }
    }

    internal void AddNetTerminalGene(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            if (x.State == NetTerminalGeneState.WaitingToSend ||
                x.State == NetTerminalGeneState.WaitingToReceive)
            {
                this.managedGenes.TryAdd(x.Gene, x);
            }
        }
    }

    internal void RemoveNetTerminalGene(NetTerminalGene[] genes)
    {
        foreach (var x in genes)
        {
            this.managedGenes.TryRemove(x.Gene, out _);
        }
    }

    internal Serilog.ILogger? TerminalLogger { get; private set; }

    private NetSocket netSocket;

    private NetTerminal.GoshujinClass terminals = new();

    private ConcurrentDictionary<ulong, NetTerminalGene> managedGenes = new();

    private ConcurrentQueue<UnmanagedSend> unmanagedSends = new();
}
