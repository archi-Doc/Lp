// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Responder;
using Netsphere.Stats;

namespace Netsphere;

public class NetTerminal
{
    public NetTerminal(UnitContext context, UnitLogger unitLogger, NetBase netBase, NetStats statsData)
    {
        this.UnitLogger = unitLogger;
        this.logger = unitLogger.GetLogger<Terminal>();
        this.NetBase = netBase;
        // this.NetSocketIpv4 = new(this);
        // this.NetSocketIpv6 = new(this);
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
}
