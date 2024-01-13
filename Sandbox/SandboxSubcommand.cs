// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using Netsphere;
using Netsphere.Block;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("sandbox")]
public class SandboxSubcommand : ISimpleCommandAsync
{
    public SandboxSubcommand(ILogger<SandboxSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.NetControl.RegisterResponder(Netsphere.Responder.MemoryResponder.Instance);
        this.NetControl.RegisterResponder(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNodeAsync(NetAddress.Alternative);
        if (netNode is null)
        {
            return;
        }

        netTerminal.PacketTerminal.MaxResendCount = 0; // tempcode
        using (var connection = await netTerminal.TryConnect(netNode))
        {
            if (connection is not null)
            {
                var testBlock = TestBlock.Create(1000);
                var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                Debug.Assert(testBlock.Equals(r.Value));
            }
        }
    }

    public NetControl NetControl { get; set; }

    private ILogger<SandboxSubcommand> logger;
}
