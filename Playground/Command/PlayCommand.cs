// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Arc.Crypto;
using System.Threading;
using Arc.Unit;
using Netsphere;
using Netsphere.Misc;
using Netsphere.Packet;
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
        this.netControl.Responders.Register(Netsphere.Responder.CreateRelayBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.netControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNode(NetAddress.Alternative);
        if (netNode is null)
        {
            return;
        }

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse).ConfigureAwait(false))
        {
            if (clientConnection is null)
            {
                return;
            }

            var block = new CreateRelayBlock((ushort)RandomVault.Pseudo.NextUInt32());
            var r = await clientConnection.SendAndReceive<CreateRelayBlock, CreateRelayResponse>(block).ConfigureAwait(false);
            if (r.IsFailure || r.Value is null)
            {
                return;
            }
            else if (r.Value.Result != RelayResult.Success)
            {
                return;
            }

            var result = netTerminal.RelayCircuit.AddRelay(netNode, block.RelayId);
            Console.WriteLine(result.ToString());
        }
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}
