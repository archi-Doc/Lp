﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("senddata")]
public class SendDataSubcommand : ISimpleCommandAsync<SendDataOptions>
{
    public SendDataSubcommand(NetControl netControl)
    {
        this.NetControl = netControl;
    }

    public async Task Run(SendDataOptions options, string[] args)
    {
        if (!SubcommandService.TryParseNodeAddress(options.Node, out var node))
        {
            return;
        }

        Logger.Priority.Information($"SendData: {node.ToString()}");

        var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.Terminal.Create(nodeInformation))
        {
            var p = new PacketPunch(null);

            /*var result = await terminal.EncryptConnectionAsync();
            if (result != NetInterfaceResult.Success)
            {
                return;
            }*/

            var t = terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"t:");
            var r = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"t: {t.Result}");
            Logger.Priority.Information($"r: {r}");

            var p2 = TestBlock.Create();
            var t2 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);
            Logger.Priority.Information($"t2: {t2}");

            // await Task.Delay(2000);

            /*var netInterface = terminal.SendAndReceive<PacketPunch, PacketPunchResponse>(p);
            if (netInterface != null)
            {
                netInterface.Receive(out var r);
            }*/
        }
    }

    public NetControl NetControl { get; set; }
}

public record SendDataOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
