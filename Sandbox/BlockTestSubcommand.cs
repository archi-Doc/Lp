// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using Netsphere;
using Netsphere.Block;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("block")]
public class BlockTestSubcommand : ISimpleCommandAsync
{
    public BlockTestSubcommand(ILogger<BlockTestSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.NetControl.NetResponder.Register(Netsphere.Responder.MemoryResponder.Instance);
        this.NetControl.NetResponder.Register(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNodeAsync(NetAddress.Alternative);
        // this.NetControl.NetBase.DefaultSendTimeout = TimeSpan.FromSeconds(5);
        if (netNode is null)
        {
            return;
        }
        
        // this.NetControl.NetTerminal.SetDeliveryFailureRatio(0.05d);
        this.NetControl.NetTerminal.PacketTerminal.MaxResendCount = 10;
        //this.NetControl.Alternative!.SetDeliveryFailureRatio(0.05d);
        this.NetControl.Alternative!.PacketTerminal.MaxResendCount = 10;
        using (var connection = await netTerminal.TryConnect(netNode))
        {
            if (connection is not null)
            {
                var testBlock = TestBlock.Create(4_000_000); // 100_000
                var r = await connection.SendAndReceive<TestBlock, TestBlock>(testBlock);
                var equals = testBlock.Equals(r.Value);

                await Console.Out.WriteLineAsync(equals.ToString());
                await Console.Out.WriteLineAsync(connection.ToString());

                await Task.Delay(500);
            }
        }
    }

    public NetControl NetControl { get; set; }

    private ILogger logger;
}
