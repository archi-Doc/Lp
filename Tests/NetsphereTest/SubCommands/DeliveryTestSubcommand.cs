// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net;
using Arc.Unit;
using Netsphere;
using Netsphere.Packet;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("delivery")]
public class DeliveryTestSubcommand : ISimpleCommandAsync<DeliveryTestOptions>
{
    private const int Count = 1_000;

    public DeliveryTestSubcommand(ILogger<DeliveryTestSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(DeliveryTestOptions options, string[] args)
    {
        var nodeString = options.Node;
        if (string.IsNullOrEmpty(nodeString))
        {
            nodeString = "alternative";
        }

        if (!NetAddress.TryParse(this.logger, nodeString, out var nodeAddress))
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        var netTerminal = this.NetControl.NetTerminal;
        netTerminal.SetDeliveryFailureRatio(0.3); // 1 - 0.3^3 = 0.973
        var packetTerminal = netTerminal.PacketTerminal;

        nodeAddress.TryCreateIPEndPoint(out var endPoint);
        endPoint = new System.Net.IPEndPoint(IPAddress.Loopback, 50000);

        sw.Restart();

        int successCount = 0;
        var array = new Task[Count];
        for (int i = 0; i < Count; i++)
        {
            array[i] = Task.Run(async () =>
            {
                var p = new PacketPing("test56789");
                var result = await packetTerminal.SendAndReceiveAsync<PacketPing, PacketPingResponse>(endPoint, p);
                if (result.Result == NetResult.Success)
                {
                    Interlocked.Increment(ref successCount);
                }
            });
        }

        await Task.WhenAll(array);

        Console.WriteLine($"{sw.ElapsedMilliseconds} ms, {successCount}/{Count}");
    }

    public NetControl NetControl { get; set; }

    private ILogger<DeliveryTestSubcommand> logger;
}

public record DeliveryTestOptions
{
    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
