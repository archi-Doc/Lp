// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Interfaces;
using Netsphere.Packet;

namespace Lp.Subcommands;

[SimpleCommand("restart")]
public class RestartCommand : ISimpleCommandAsync<RestartOptions>
{
    private const int WaitIntervalInSeconds = 20;
    private const int PingIntervalInSeconds = 1;
    private const int PingRetries = 7;

    public RestartCommand(ILogger<RestartCommand> logger, NetTerminal terminal)
    {
        this.logger = logger;
        this.netTerminal = terminal;
    }

    public async Task RunAsync(RestartOptions options, string[] args)
    {
        options.Prepare();
        this.logger.TryGet()?.Log($"{options.ToString()}");

        if (options.RemoteSeedKey is not { } privateKey)
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not parse remote secret key");
            return;
        }

        var list = options.RunnerNode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var nodeList = new List<NetNode>();
        foreach (var x in list)
        {
            if (NetNode.TryParseNetNode(this.logger, x, out var node))
            {
                nodeList.Add(node);
            }
        }

        var success = 0;
        await Parallel.ForEachAsync(nodeList, async (netNode, cancellationToken) =>
        {
            // Ping container
            var address = new NetAddress(netNode.Address, options.ContainerPort);
            if (await this.Ping(address) == false)
            {// No ping
                return;
            }

            // Restart
            using (var connection = await this.netTerminal.Connect(netNode))
            {
                if (connection == null)
                {
                    this.logger.TryGet()?.Log($"Could not connect {netNode.ToString()}");
                    return;
                }

                var token = new AuthenticationToken(connection.EmbryoSalt);
                NetHelper.Sign(token, privateKey);
                var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
                if (result != NetResult.Success)
                {
                    return;
                }

                var service = connection.GetService<IRemoteControl>();
                result = await service.Restart();
                this.logger.TryGet()?.Log($"Restart({result}): {netNode.Address.ToString()}");
                if (result != NetResult.Success)
                {
                    return;
                }
            }

            // Wait
            // this.logger.TryGet()?.Log($"Waiting...");
            await Task.Delay(TimeSpan.FromSeconds(WaitIntervalInSeconds));

            // Ping container
            var sec = PingIntervalInSeconds;
            for (var i = 0; i < PingRetries; i++)
            {
                if (await this.Ping(address))
                {
                    success++;
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(sec));
                sec *= 2;
            }
        });

        this.logger.TryGet()?.Log($"Restart Success/Total: {success}/{nodeList.Count}");
    }

    private async Task<bool> Ping(NetAddress address)
    {
        var r = await this.netTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(address, new());
        this.logger.TryGet()?.Log($"Ping({r.Result}): {address.ToString()}");

        if (r.Result == NetResult.Success)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
}
