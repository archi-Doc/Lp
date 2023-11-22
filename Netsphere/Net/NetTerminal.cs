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

        this.netSender = new(this, unitLogger.GetLogger<NetSender>());
        this.PacketTerminal = new(this, unitLogger.GetLogger<PacketTerminal>());
        this.connections = new(netStats);
        this.netStats = netStats;

        this.ResponseTimeout = TimeSpan.FromSeconds(DefaultResponseTimeoutInSeconds);
    }

    #region FieldAndProperty

    public CancellationToken CancellationToken
        => ThreadCore.Root.CancellationToken;

    public NetBase NetBase { get; }

    public bool IsAlternative { get; }

    public string NetTerminalString => this.IsAlternative ? "Alt" : "Main";

    public int Port { get; set; }

    public PacketTerminal PacketTerminal { get; }

    public TimeSpan ResponseTimeout { get; set; }

    internal UnitLogger UnitLogger { get; private set; }

    private readonly ILogger logger;
    private readonly NetStats netStats;
    private readonly NetSender netSender;
    private readonly NetConnectionControl connections;
    private NodePrivateKey nodePrivateKey = default!;

    #endregion

    public NetConnection? TryConnect(NetAddress address, NetConnection.ConnectMode mode = NetConnection.ConnectMode.ReuseClosed)
        => this.connections.TryConnect(address, mode);

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (this.Port == 0)
        {
            this.Port = this.NetBase.NetsphereOptions.Port;
        }

        if (!this.IsAlternative)
        {
            this.nodePrivateKey = this.NetBase.NodePrivateKey;
            this.Port = 49999; // tempcode
        }
        else
        {
            this.nodePrivateKey = NodePrivateKey.AlternativePrivateKey;
            this.Port = NetAddress.Alternative.Port;
            this.Port = 50000; // tempcode
        }

        // Responders
        // DefaultResponder.Register(this.Terminal);
    }

    async Task IUnitExecutable.RunAsync(UnitMessage.RunAsync message)
    {
        var core = message.ParentCore;
        this.netSender.Start(core);
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.netSender.Stop();
    }

    internal void ProcessSend(NetSender netSender)
    {
        // tempcode
        this.PacketTerminal.ProcessSend(netSender);
    }

    internal unsafe void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.Owner toBeShared, int packetSize)
    {
        var currentSystemMics = this.netSender.CurrentSystemMics;
        var owner = toBeShared.ToMemoryOwner(0, packetSize);

        // tempcode
        this.PacketTerminal.ProcessReceive(endPoint, owner, currentSystemMics);
    }
}
