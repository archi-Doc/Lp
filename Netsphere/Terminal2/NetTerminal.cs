// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Netsphere.Crypto;
using Netsphere.Stats;

namespace Netsphere;

public class NetTerminal
{
    public NetTerminal(UnitLogger unitLogger, NetBase netBase, NetStats statsData)
    {
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;

        this.NetSocketIpv4 = new(this.ProcessSend, this.ProcessReceive);
        this.NetSocketIpv6 = new(this.ProcessSend, this.ProcessReceive);
        this.statsData = statsData;
    }

    public NetBase NetBase { get; }

    public bool IsAlternative { get; private set; }

    public int Port { get; set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal NetSocket NetSocketIpv4 { get; private set; }

    internal NetSocket NetSocketIpv6 { get; private set; }

    internal UnitLogger UnitLogger { get; private set; }

#pragma warning disable SA1401 // Fields should be private
    internal int SendCapacityPerRound;
#pragma warning restore SA1401 // Fields should be private

    private readonly ILogger logger;
    private readonly NetStats statsData;

    public void Prepare(UnitMessage.Prepare message)
    {
        // Terminals
        this.Terminal.Initialize(false, this.NetBase.NodePrivateKey);
        if (this.Alternative != null)
        {
            this.Alternative.Initialize(true, NodePrivateKey.AlternativePrivateKey);
            this.Alternative.Port = NetAddress.Alternative.Port;
        }

        // Responders
        // DefaultResponder.Register(this.Terminal);
    }

    private void ProcessSend(long currentMics)
    {
    }

    private unsafe void ProcessReceive(IPEndPoint endPoint, ByteArrayPool.Owner arrayOwner, int packetSize, long currentMics)
    {
    }
}
