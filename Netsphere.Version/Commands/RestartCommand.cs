// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Interfaces;
using Netsphere.Packet;

namespace LP.Subcommands;

[SimpleCommand("restart")]
public class RestartCommand : ISimpleCommandAsync<RestartOptions>
{
    private const int WaitIntervalInSeconds = 10;
    private const int PingIntervalInSeconds = 1;
    private const int PingRetries = 5;

    public RestartCommand(ILogger<RestartCommand> logger, NetTerminal terminal)
    {
        this.logger = logger;
        this.netTerminal = terminal;
    }

    public async Task RunAsync(RestartOptions options, string[] args)
    {//
        if (await NetHelper.TryGetNetNode(this.netTerminal, options.Node) is not { } netNode)
        {
            return;
        }

        // Parallel.For

        if (!CryptoHelper.TryParseFromSourceOrEnvironmentVariable<SignaturePrivateKey>(options.RemotePrivateKeyString, NetConstants.RemotePrivateKeyName, out var privateKey))
        {
            return;
        }

        // Ping container
        this.containerAddress = new(netNode.Address, options.ContainerPort);
        if (await this.Ping() == false)
        {
            this.logger.TryGet(LogLevel.Error)?.Log("NoPingFromContainer");
        }

        // Restart
        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log($"Could not connect {netNode.ToString()}");
                return;
            }

            var token = new AuthenticationToken(connection.Salt);
            NetHelper.Sign(token, privateKey);
            var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                this.logger.TryGet(LogLevel.Error)?.Log("Error.Authorization");
                return;
            }

            var service = connection.GetService<IRemoteControl>();
            result = await service.Restart();
            this.logger.TryGet()?.Log($"Restart: {result}");
            if (result != NetResult.Success)
            {
                return;
            }
        }

        // Wait
        this.logger.TryGet()?.Log($"Waiting...");
        await Task.Delay(TimeSpan.FromSeconds(WaitIntervalInSeconds));

        // Ping container
        var sec = PingIntervalInSeconds;
        for (var i = 0; i < PingRetries; i++)
        {
            if (await this.Ping())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(sec));
            sec *= 2;
        }
    }

    private async Task<bool> Ping()
    {
        var r = await this.netTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(this.containerAddress, new());
        this.logger.TryGet()?.Log($"Ping {this.containerAddress.ToString()}: {r.Result}");

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
    private NetAddress containerAddress;
}
