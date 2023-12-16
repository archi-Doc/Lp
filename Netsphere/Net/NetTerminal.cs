// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Net;
using Netsphere.Packet;
using Netsphere.Stats;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Netsphere;

public class NetTerminal : UnitBase, IUnitPreparable, IUnitExecutable
{
    public const double DefaultResponseTimeoutInSeconds = 2d;

    public NetTerminal(bool isAlternative, UnitContext unitContext, UnitLogger unitLogger, NetBase netBase, NetStats netStats)
        : base(unitContext)
    {
        this.IsAlternative = isAlternative;
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;
        this.NetStats = netStats;

        this.NetSender = new(this, unitLogger.GetLogger<NetSender>());
        this.PacketTerminal = new(this.NetBase, this.NetStats, this, unitLogger.GetLogger<PacketTerminal>());
        this.ConnectionTerminal = new(this);
        this.netCleaner = new(this);

        this.ResponseTimeout = TimeSpan.FromSeconds(DefaultResponseTimeoutInSeconds);
    }

    #region FieldAndProperty

    public CancellationToken CancellationToken
        => ThreadCore.Root.CancellationToken;

    public NetBase NetBase { get; }

    public string NetTerminalString => this.IsAlternative ? "Alt" : "Main";

    public NetStats NetStats { get; }

    public PacketTerminal PacketTerminal { get; }

    public bool IsAlternative { get; }

    public int Port { get; set; }

    public TimeSpan ResponseTimeout { get; set; }

    internal NetSender NetSender { get; }

    internal UnitLogger UnitLogger { get; private set; }

    internal ConnectionTerminal ConnectionTerminal { get; private set; }

    private readonly ILogger logger;
    private readonly NetCleaner netCleaner;

    private NodePrivateKey nodePrivateKey = default!;

    #endregion

    public void Clean()
    {
        this.ConnectionTerminal.Clean();
    }

    public bool TryCreateEndPoint(in NetAddress address, out NetEndPoint endPoint)
        => this.NetStats.TryCreateEndPoint(in address, out endPoint);

    public void SetDeliveryFailureRatio(double ratio)
    {
#if DEBUG
        this.NetSender.SetDeliveryFailureRatio(ratio);
#endif
    }

    public async Task<NetNode?> UnsafeGetNetNodeAsync(NetAddress address)
    {
        var t = await this.PacketTerminal.SendAndReceiveAsync<PacketGetInformation, PacketGetInformationResponse>(address, new()).ConfigureAwait(false);
        if (t.Value is null)
        {
            return null;
        }

        return new(address, t.Value.PublicKey);
    }

    public Task<ClientConnection?> TryConnect(NetNode node, Connection.ConnectMode mode = Connection.ConnectMode.ReuseClosed)
        => this.ConnectionTerminal.TryConnect(node, mode);

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (this.Port == 0)
        {
            this.Port = this.NetBase.NetsphereOptions.Port;
        }

        if (!this.IsAlternative)
        {
            this.nodePrivateKey = this.NetBase.NodePrivateKey;
        }
        else
        {
            this.nodePrivateKey = NodePrivateKey.AlternativePrivateKey;
            this.Port = NetAddress.Alternative.Port;
        }

        // Responders
        // DefaultResponder.Register(this.Terminal);
    }

    async Task IUnitExecutable.RunAsync(UnitMessage.RunAsync message)
    {
        var core = message.ParentCore;
        this.NetSender.Start(core);
        this.netCleaner.Start(core);
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.NetSender.Stop();
        this.netCleaner.Stop();
    }

    internal void ProcessSend(NetSender netSender)
    {
        // 1st: Packets
        this.PacketTerminal.ProcessSend(netSender);
        if (!netSender.CanSend)
        {
            return;
        }

        // 2nd: Genes (NetTransmission)
        this.ConnectionTerminal.ProcessSend(netSender);
        if (!netSender.CanSend)
        {
            return;
        }
    }

    internal unsafe void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.Owner toBeShared, int packetSize)
    {
        var currentSystemMics = this.NetSender.CurrentSystemMics;
        var owner = toBeShared.ToMemoryOwner(0, packetSize);
        var span = owner.Span;

        if (packetSize < PacketHeader.Length)
        {// Check length
            return;
        }

        // Engagement
        span = span.Slice(4);
        var engagement = BitConverter.ToUInt16(span);

        // Packet type
        span = span.Slice(2);
        var packetType = BitConverter.ToUInt16(span);

        if (packetType < 256)
        {// Packet
            this.PacketTerminal.ProcessReceive(endPoint, packetType, owner, currentSystemMics);
        }
        else if (packetType < 511)
        {// Gene
            this.ConnectionTerminal.ProcessReceive(endPoint, packetType, owner, currentSystemMics);
        }
    }
}
