// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using LP.Net;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("senddata")]
public class SendDataSubcommand : ISimpleCommandAsync<SendDataOptions>
{
    public SendDataSubcommand(NetControl control)
    {
        this.Control = control;
    }

    public async Task Run(SendDataOptions options, string[] args)
    {
        var nodeName = options.Node;
        if (string.IsNullOrEmpty(nodeName))
        {
            nodeName = "alternative";
        }

        if (!SubcommandService.TryParseNodeAddress(nodeName, out var node))
        {
            return;
        }

        Logger.Priority.Information($"SendData: {node.ToString()}");

        var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.Control.Netsphere.Terminal.Create(nodeInformation))
        {
            var p = new RawPacketPunch(null);
            var netInterface = terminal.SendAndReceive<RawPacketPunch, RawPacketPunchResponse>(p);
            if (netInterface != null)
            {
                netInterface.Receive(out var r);
            }
        }
    }

    public NetControl Control { get; set; }
}

public record SendDataOptions
{
    [SimpleOption("node", description: "Node address")]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
