// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("restart-remote-container")]
public class RestartRemoteContainerSubcommand : ISimpleCommandAsync<RestartRemoteContainerOptions>
{
    public RestartRemoteContainerSubcommand(ILogger<RestartRemoteContainerSubcommand> logger, NetTerminal terminal, AuthorityVault authorityVault)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(RestartRemoteContainerOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var nodeInformation))
        {
            return;
        }

        var authority = await this.authorityVault.GetAuthority(options.Authority);
        if (authority == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }

        // using (var terminal = await this.terminal.CreateAndEncrypt(nodeInformation))
        this.logger.TryGet()?.Log($"Start");
        using (var connection = await this.terminal.Connect(nodeInformation))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, nodeInformation.ToString());
                return;
            }

            var token = new AuthenticationToken(connection.Salt);
            authority.SignToken(token);
            if (!connection.ValidateAndVerifyWithSalt(token))
            {
                return;
            }

            var service = connection.GetService<IRemoteControlService>();
            var response = await service.Authenticate(token).ResponseAsync;
            var result = response.Result;
            this.logger.TryGet()?.Log($"Authenticate: {result}");

            if (result == NetResult.Success)
            {
                result = await service.Restart();
                this.logger.TryGet()?.Log($"Restart: {result}");
            }
        }
    }

    private readonly ILogger logger;
    private readonly NetTerminal terminal;
    private readonly AuthorityVault authorityVault;
}

public record RestartRemoteContainerOptions
{
    [SimpleOption("node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Private key for remote operation")]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
