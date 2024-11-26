// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Interfaces;
using Netsphere.Packet;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("restart-remote-container")]
public class RestartRemoteContainerSubcommand : ISimpleCommandAsync<RestartRemoteContainerOptions>
{
    private const int WaitIntervalInSeconds = 10;
    private const int PingIntervalInSeconds = 1;
    private const int PingRetries = 5;

    public RestartRemoteContainerSubcommand(ILogger<RestartRemoteContainerSubcommand> logger, NetTerminal terminal)
    {
        this.logger = logger;
        this.netTerminal = terminal;
    }

    public async Task RunAsync(RestartRemoteContainerOptions options, string[] args)
    {
        if (await NetHelper.TryGetNetNode(this.netTerminal, options.RunnerNode) is not { } netNode)
        {
            return;
        }

        if (!CryptoHelper.TryParseFromSourceOrEnvironmentVariable<SeedKey>(options.RemotePrivault, NetConstants.RemotePrivateKeyName, out var seedKey))
        {
            return;
        }

        // Ping container
        this.containerAddress = new(netNode.Address, options.ContainerPort);
        if (await this.Ping() == false)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoPingFromContainer);
        }

        /*var authority = await this.authorityControl.GetAuthority(options.Authority);
        if (authority == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }*/

        // Restart
        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, netNode.ToString());
                return;
            }

            var token = new AuthenticationToken(connection.Salt);
            NetHelper.Sign(token, seedKey);
            var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Authorization);
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

public record RestartRemoteContainerOptions
{
    [SimpleOption("RunnerNode", Description = "Runner node", Required = true)]
    public string RunnerNode { get; init; } = string.Empty;

    [SimpleOption("RemotePrivault", Description = "Private key or vault name for remote operation")]
    public string RemotePrivault { get; init; } = string.Empty;

    [SimpleOption("ContainerPort", Description = "Port number associated with the container")]
    public ushort ContainerPort { get; init; } = NetConstants.MinPort;
}
