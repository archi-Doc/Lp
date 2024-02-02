// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using LP.T3CS;
using Netsphere;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("restart")]
public class RemoteSubcommandRestart : ISimpleCommandAsync<RemoteSubcommandRestartOptions>
{
    public RemoteSubcommandRestart(ILogger<RemoteSubcommandRestart> logger, NetTerminal terminal, AuthorityVault authorityVault)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(RemoteSubcommandRestartOptions options, string[] args)
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
        using (var connection = await this.terminal.TryConnect(nodeInformation))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, nodeInformation.ToString());
                return;
            }

            var token = new AuthenticationToken(connection.Salt);
            authority.SignToken(token);
            if (!connection.ValidateAndVerify(token))
            {
                return;
            }

            var service = connection.GetService<RemoteControlService>();
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

    private ILogger logger;
    private NetTerminal terminal;
    private AuthorityVault authorityVault;
}

public record RemoteSubcommandRestartOptions
{
    [SimpleOption("authority", Description = "Authority", Required = true)]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;
}
