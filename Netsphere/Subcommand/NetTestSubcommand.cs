// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP.Subcommands;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("nettest")]
public class NetTestSubcommand : ISimpleCommandAsync<NetTestOptions>
{
    public NetTestSubcommand(ILogger<NetTestSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(NetTestOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeAddress(this.logger, options.Node, out var node))
        {
            return;
        }

        this.logger.TryGet()?.Log($"SendData: {node.ToString()}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.Terminal.Create(node))
        {
            // await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch());

            var p = new PacketPunch(null);

            var result = await terminal.EncryptConnectionAsync();

            this.logger.TryGet()?.Log($"{result.ToString()}");
            if (result != NetResult.Success)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            /*var t = terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"t: {t.Result}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");*/

            sw.Restart();
            var t5 = terminal.SendPacketAndReceiveAsync<TestPacket, TestPacket>(TestPacket.Create(11));
            this.logger.TryGet()?.Log($"t5: {t5.Result}");
            this.logger.TryGet()?.Log($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            var p2 = TestBlock.Create(400_000);
            sw.Restart();
            var t2 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);
            this.logger.TryGet()?.Log($"tt: {t2.Result}");
            this.logger.TryGet()?.Log($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            /*p2 = TestBlock.Create(2000);
            Logger.Priority.Information($"p2b send: {p2}");
            var t3 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);
            Logger.Priority.Information($"t2 received: {t2.Value}");
            Logger.Priority.Information($"t3 received: {t3.Value}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");*/

            /*var p4 = TestBlock.Create(4_000_000);
            Logger.Priority.Information($"4MB send: {p4}");
            sw.Restart();
            var t4 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p4, int.MaxValue);
            Logger.Priority.Information($"4MB received: {t4.Value}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");*/

            /*var p4 = TestBlock.Create(4000_000);
            Logger.Priority.Information($"4MB send: {p4}");
            sw.Restart();
            // var t4 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p4);
            // Logger.Priority.Information($"4MB received: {t4.Value}");
            var result = await terminal.SendAsync<TestBlock>(p4);
            Logger.Priority.Information(result.ToString());
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");*/
        }
    }

    public NetControl NetControl { get; set; }

    private ILogger<NetTestSubcommand> logger;
}

public record NetTestOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
