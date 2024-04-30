﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using System.Threading;
using Arc.Unit;
using Netsphere;
using Netsphere.Packet;
using SimpleCommandLine;
using Netsphere.Crypto;
using Netsphere.Relay;

namespace Playground;

[SimpleCommand("play")]
public class PlayCommand : ISimpleCommandAsync
{
    public PlayCommand(ILogger<PlayCommand> logger, NetControl netControl, IRelayControl relayControl)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.netControl.Responders.Register(Netsphere.Responder.MemoryResponder.Instance);
        this.netControl.Responders.Register(Netsphere.Responder.TestBlockResponder.Instance);

        var sw = Stopwatch.StartNew();
        var netTerminal = this.netControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var privateKey = SignaturePrivateKey.Create();
        if (this.relayControl is CertificateRelayControl rc)
        {
            rc.SetCertificatePublicKey(privateKey.ToPublicKey());
        }

        var netNode = await netTerminal.UnsafeGetNetNode(NetAddress.Alternative);
        if (netNode is null)
        {
            return;
        }

        using (var clientConnection = await netTerminal.ConnectForRelay(netNode, 0))
        {
            if (clientConnection is null)
            {
                return;
            }

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
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

            var result = netTerminal.RelayCircuit.AddRelay(r.Value.RelayId, clientConnection, true);
            Console.WriteLine(result.ToString());
            Console.WriteLine(netTerminal.RelayCircuit.NumberOfRelays);
        }

        // using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse, 1))
        using (var clientConnection = await netTerminal.ConnectForRelay(netNode, 1))
        {
            if (clientConnection is null)
            {
                return;
            }

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
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

            var result = netTerminal.RelayCircuit.AddRelay(r.Value.RelayId, clientConnection, true);
            Console.WriteLine(result.ToString());
            Console.WriteLine(netTerminal.RelayCircuit.NumberOfRelays);

            var setRelayPacket = new SetRelayPacket();
            setRelayPacket.OuterEndPoint = new(r.Value.RelayId, clientConnection.DestinationEndpoint.EndPoint);
            await netTerminal.PacketTerminal.SendAndReceive<SetRelayPacket, SetRelayResponse>(NetAddress.Relay, setRelayPacket, -1);
        }

        // using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse, 1))
        // using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.ReuseIfAvailable, 2))

        using (var clientConnection = await netTerminal.Connect(netNode))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();
            var result = await service.DoubleString("Test2");
            Console.WriteLine(result);
        }

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.ReuseIfAvailable, 2))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();
            var result = await service.DoubleString("Test1");
            Console.WriteLine(result);
        }

        /*var rr = await netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), -1);
        Console.WriteLine(rr);*/
        /*var rr = await netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), -2);
        Console.WriteLine(rr);*/

        // Console.WriteLine(netTerminal.RelayCircuit.UnsafeToString());
        Console.WriteLine(await netTerminal.RelayCircuit.UnsafeDetailedToString());
        /*for (var i = 0; i < 10; i++)
        {
            Console.WriteLine(await netTerminal.RelayCircuit.UnsafeDetailedToString());
            await Task.Delay(2_000 * i);
        }*/
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
    private readonly IRelayControl relayControl;
}
