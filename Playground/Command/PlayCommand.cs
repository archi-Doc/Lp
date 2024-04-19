// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Arc.Unit;
using Netsphere;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Playground;

[SimpleCommand("play")]
public class PlayCommand : ISimpleCommandAsync
{
    public PlayCommand(ILogger<PlayCommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.netControl.Responders.Register(Netsphere.Responder.MemoryResponder.Instance);
        this.netControl.Responders.Register(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.netControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNode(NetAddress.Alternative);
        if (netNode is null)
        {
            return;
        }

        // netTerminal.PacketTerminal.MaxResendCount = 0;
        using (var connection = await netTerminal.Connect(netNode))
        {
            if (connection is not null)
            {
                var testBlock = NetTestBlock.Create(1000);
                var r = await connection.SendAndReceive<NetTestBlock, NetTestBlock>(testBlock);
                Debug.Assert(testBlock.Equals(r.Value));
            }
        }
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}
