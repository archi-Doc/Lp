// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Stats;

namespace Netsphere;

public class NetTerminal : UnitBase, IUnitPreparable, IUnitExecutable
{
    public NetTerminal(bool isAlternative, UnitContext unitContext, UnitLogger unitLogger, NetBase netBase, NetStats statsData)
        : base(unitContext)
    {
        this.IsAlternative = isAlternative;
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;

        this.netSocketIpv4 = new(this.ProcessSend, this.ProcessReceive);
        this.netSocketIpv6 = new(this.ProcessSend, this.ProcessReceive);
        this.statsData = statsData;
    }

    public NetBase NetBase { get; }

    public bool IsAlternative { get; }

    public int Port { get; set; }

    internal UnitLogger UnitLogger { get; private set; }

    private readonly ILogger logger;
    private readonly NetStats statsData;
    private readonly NetSocket netSocketIpv4;
    private readonly NetSocket netSocketIpv6;
    private NodePrivateKey nodePrivateKey = default!;

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
        if (!this.netSocketIpv4.Start(core, this.Port, false))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {this.Port}.");
            throw new PanicException();
        }

        if (!this.netSocketIpv6.Start(core, this.Port, true))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {this.Port}.");
            throw new PanicException();
        }
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.netSocketIpv4.Stop();
        this.netSocketIpv6.Stop();
    }

    private void ProcessSend(long currentMics)
    {
    }

    private unsafe void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.Owner arrayOwner, int packetSize, long currentMics)
    {
    }
}
