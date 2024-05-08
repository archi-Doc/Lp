// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

[SimpleCommand("relay", Default = true)]
public class RelayCommand : ISimpleCommandAsync
{
    public RelayCommand(ILogger<RelayCommand> logger, NetControl netControl, IRelayControl relayControl)
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

            var packet = RelayOperatioPacket.CreateSetOuterEndPoint(new(r.Value.RelayId, clientConnection.DestinationEndpoint.EndPoint));
            await netTerminal.PacketTerminal.SendAndReceive<RelayOperatioPacket, RelayOperatioResponse>(NetAddress.Relay, packet, -1);
        }

        // using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse, 1))
        // using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.ReuseIfAvailable, 2))

        Console.WriteLine(await netTerminal.RelayCircuit.UnsafeDetailedToString());

        /*sing (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.ReuseIfAvailable, 2))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();
            var result = await service.DoubleString("Test2");
            Console.WriteLine(result);
        }*/

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse, 2))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();
            /*var token = new CertificateToken<ConnectionAgreement>(clientConnection.Agreement with { MinimumConnectionRetentionMics = Mics.FromMinutes(10), });
            var rr = await service.UpdateAgreement(token);
            var result = await service.DoubleString("Test1");
            Console.WriteLine(result);*/

            var count = 0;
            for (var i = 0; i < 1; i++)
            {
                this.logger.TryGet()?.Log("Pingpong");
                var bin = new byte[5000];
                bin.AsSpan().Fill(0x12);
                var result = await service.Pingpong(bin);
                // await Task.Delay(100);
                this.logger.TryGet()?.Log((result is not null).ToString());
                if (result is not null &&
                    result.SequenceEqual(bin))
                {
                    count++;
                }
            }

            Console.WriteLine(count);

            /*await Task.Delay(1000);
            this.logger.TryGet()?.Log("Pingpong");
            var bin2 = new byte[3000];
            bin2.AsSpan().Fill(0x12);
            var result2 = await service.Pingpong(bin2);
            this.logger.TryGet()?.Log((result2 is not null).ToString());*/
        }

        /*var rr = await netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), -1);
        Console.WriteLine(rr);*/
        /*var rr = await netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), -2);
        Console.WriteLine(rr);*/

        // Console.WriteLine(netTerminal.RelayCircuit.UnsafeToString());
        // Console.WriteLine(await netTerminal.RelayCircuit.UnsafeDetailedToString());

        netTerminal.RelayCircuit.Clear();
        Console.WriteLine(await netTerminal.RelayCircuit.UnsafeDetailedToString());
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
    private readonly IRelayControl relayControl;
}
