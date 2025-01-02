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
using System.Runtime.CompilerServices;

namespace Playground;

[SimpleCommand("relay")]
public class RelayCommand : ISimpleCommandAsync
{
    public static bool BreakpointFlag { get; set; } = false;

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

        var seedKey = SeedKey.NewSignature();
        if (this.relayControl is CertificateRelayControl rc)
        {
            rc.SetCertificatePublicKey(seedKey.GetSignaturePublicKey());
        }

        var netNode = await netTerminal.UnsafeGetNetNode(Alternative.NetAddress);
        if (netNode is null)
        {
            return;
        }

        using (var relayConnection = await netTerminal.ConnectForRelay(netNode, false, 0))
        {
            if (relayConnection is null)
            {
                return;
            }

            var block = new AssignRelayBlock();
            var token = new CertificateToken<AssignRelayBlock>(block);
            relayConnection.SignWithSalt(token, seedKey);
            var r = await relayConnection.SendAndReceive<CertificateToken<AssignRelayBlock>, AssignRelayResponse>(token).ConfigureAwait(false);
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

            var result = await netTerminal.OutgoingCircuit.AddRelay(block, r.Value, relayConnection);
            Console.WriteLine(result.ToString());
            Console.WriteLine($"{netTerminal.OutgoingCircuit.NumberOfRelays} relays");
            Console.WriteLine();
        }

        using (var relayConnection = await netTerminal.ConnectForRelay(netNode, false, 1))
        {
            if (relayConnection is null)
            {
                return;
            }

            var block = new AssignRelayBlock();
            var token = new CertificateToken<AssignRelayBlock>(block);
            relayConnection.SignWithSalt(token, seedKey);
            var r = await relayConnection.SendAndReceive<CertificateToken<AssignRelayBlock>, AssignRelayResponse>(token).ConfigureAwait(false);
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

            var result = await netTerminal.OutgoingCircuit.AddRelay(block, r.Value, relayConnection);
            await Task.Delay(10);
            Console.WriteLine(result.ToString());
            Console.WriteLine($"{netTerminal.OutgoingCircuit.NumberOfRelays} relays");
            Console.WriteLine();

            var service2 = relayConnection.GetService<ITestService>();
            var rr2 = await service2.DoubleString("Hello2");
            Console.WriteLine($"{rr2}");
        }

        BreakpointFlag = true;
        Console.WriteLine(await netTerminal.OutgoingCircuit.UnsafeDetailedToString());

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse, 2))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();

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
        }

        await netTerminal.OutgoingCircuit.Close();
        Console.WriteLine(await netTerminal.OutgoingCircuit.UnsafeDetailedToString());
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
    private readonly IRelayControl relayControl;
}
