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
using Netsphere.Crypto;

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

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse).ConfigureAwait(false))
        {
            if (clientConnection is null)
            {
                return;
            }

            var block = new CreateRelayBlock((ushort)RandomVault.Pseudo.NextUInt32());
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, SignaturePrivateKey.Create());
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token).ConfigureAwait(false);
            if (r.IsFailure || r.Value is null)
            {
                Console.WriteLine(r.Result.ToString());
                return;
            }
            else if (r.Value.Result != RelayResult.Success)
            {
                Console.WriteLine(r.Result.ToString());
                return;
            }

            var result = netTerminal.RelayCircuit.AddRelay(netNode, block.RelayId);
            Console.WriteLine(result.ToString());
        }
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}
