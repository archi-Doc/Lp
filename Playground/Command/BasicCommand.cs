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

[SimpleCommand("basic")]
public class BasicCommand : ISimpleCommandAsync
{
    public BasicCommand(ILogger<BasicCommand> logger, NetUnit netUnit, IRelayControl relayControl)
    {
        this.logger = logger;
        this.netUnit = netUnit;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(string[] args)
    {
        var sw = Stopwatch.StartNew();
        var netTerminal = this.netUnit.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNode(Alternative.NetAddress);
        if (netNode is null)
        {
            return;
        }

        using (var clientConnection = await netTerminal.Connect(netNode, Connection.ConnectMode.ReuseIfAvailable, 0))
        {
            if (clientConnection is null)
            {
                return;
            }

            var service = clientConnection.GetService<ITestService>();
            var token = new CertificateToken<ConnectionAgreement>(clientConnection.Agreement with { MinimumConnectionRetentionMics = Mics.FromMinutes(1), });
            var seedKey = SeedKey.NewSignature();
            clientConnection.SignWithSalt(token, seedKey);
            var rr = await service.UpdateAgreement(token);
            var result3 = await service.DoubleString("Test1");
            Console.WriteLine(result3);

            this.logger.TryGet()?.Log("Pingpong");
            var bin = new byte[1000];
            bin.AsSpan().Fill(0x12);
            var result = await service.Pingpong(bin);
            this.logger.TryGet()?.Log((result is not null).ToString());

            this.logger.TryGet()?.Log("Pingpong");
            var bin2 = new byte[2000];
            bin.AsSpan().Fill(0x12);
            var result2 = await service.Pingpong(bin2);
            this.logger.TryGet()?.Log((result2 is not null).ToString());
        }
    }

    private readonly NetUnit netUnit;
    private readonly ILogger logger;
    private readonly IRelayControl relayControl;
}
