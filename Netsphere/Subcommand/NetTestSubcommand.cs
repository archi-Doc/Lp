// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere;
using Netsphere.Block;
using SimpleCommandLine;

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
        if (!NetAddress.TryParse(this.logger, options.Node, out var address))
        {
            return;
        }

        this.logger.TryGet()?.Log($"SendData: {address.ToString()}");

        /*using (var terminal = this.NetControl.TerminalObsolete.TryCreate(address))
        {
            if (terminal is null)
            {
                return;
            }

            // await terminal.SendAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch());

            var p = new PacketPunchObsolete(default);

            var result = await terminal.EncryptConnectionAsync();

            this.logger.TryGet()?.Log($"{result.ToString()}");
            if (result != NetResult.Success)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            sw.Restart();
            var t5 = terminal.SendPacketAndReceiveAsync<TestPacket, TestPacket>(TestPacket.Create(11));
            this.logger.TryGet()?.Log($"t5: {t5.Result}");
            this.logger.TryGet()?.Log($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");

            var p2 = TestBlock.Create(400_000);
            sw.Restart();
            var t2 = await terminal.SendAndReceiveAsync<TestBlock, TestBlock>(p2);
            this.logger.TryGet()?.Log($"tt: {t2.Result}");
            this.logger.TryGet()?.Log($"{sw.ElapsedMilliseconds} ms, Resend: {terminal.ResendCount}");
        }*/
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
