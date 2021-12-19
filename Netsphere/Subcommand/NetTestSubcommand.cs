// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("nettest")]
public class NetTestSubcommand : ISimpleCommandAsync<NetTestOptions>
{
    public NetTestSubcommand(NetControl netControl)
    {
        this.NetControl = netControl;
    }

    public async Task Run(NetTestOptions options, string[] args)
    {
        if (!SubcommandService.TryParseNodeAddress(options.Node, out var node))
        {
            return;
        }

        Logger.Priority.Information($"SendData: {node.ToString()}");

        // var nodeInformation = NodeInformation.Alternative;
        using (var terminal = this.NetControl.Terminal.Create(node))
        {
            await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch());

            var p = new PacketPunch(null);

            /*var result = await terminal.EncryptConnectionAsync();
            if (result != NetResult.Success)
            {
                return;
            }*/

            var sw = Stopwatch.StartNew();
            var t = terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(p);
            Logger.Priority.Information($"t: {t.Result}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            sw.Restart();
            var t5 = terminal.SendPacketAndReceiveAsync<TestPacket, TestPacket>(TestPacket.Create(11));
            Logger.Priority.Information($"t5: {t5.Result}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            var p2 = TestBlock.Create(4_000_000);
            BlockService.TrySerialize(p2, out var owner);
            Logger.Priority.Information($"p2 send: {p2} ({owner.Memory.Length})");
            sw.Restart();
            var t2 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);

            p2 = TestBlock.Create(2000);
            BlockService.TrySerialize(p2, out owner);
            Logger.Priority.Information($"p2b send: {p2} ({owner.Memory.Length})");
            var t3 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);
            Logger.Priority.Information($"t2 received: {t2.Value}");
            Logger.Priority.Information($"t3 received: {t3.Value}");
            Logger.Priority.Information($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            /*var netInterface = terminal.SendAndReceive<PacketPunch, PacketPunchResponse>(p);
            if (netInterface != null)
            {
                netInterface.Receive(out var r);
            }*/
        }
    }

    public NetControl NetControl { get; set; }
}

public record NetTestOptions
{
    [SimpleOption("node", description: "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
