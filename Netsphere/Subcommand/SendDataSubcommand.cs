// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
            /*var result = await terminal.EncryptConnectionAsync();
            if (result != NetInterfaceResult.Success)
            {
                return;
            }*/

            var p = new PacketPunch(null);
            var r = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"r: {r}");
            Logger.Priority.Information($"");

            r = await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"r: {r}");

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
