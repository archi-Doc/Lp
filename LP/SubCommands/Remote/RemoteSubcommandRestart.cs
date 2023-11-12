// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using LP.T3CS;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("restart")]
public class RemoteSubcommandRestart : ISimpleCommandAsync<RemoteSubcommandRestartOptions>
{
    public RemoteSubcommandRestart(ILogger<RemoteSubcommandRestart> logger, Terminal terminal, AuthorityVault authorityVault)
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
        using (var terminal = await this.terminal.CreateAndEncrypt(nodeInformation))
        {
            if (terminal == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, nodeInformation.ToString());
                return;
            }

            var token = await terminal.CreateToken(Token.Type.Authorize);
            if (token == null)
            {
                return;
            }

            authority.SignToken(token);
            if (!token.ValidateAndVerifyWithoutPublicKey())
            {
                return;
            }

            var service = terminal.GetService<RemoteControlService>();
            var response = await service.RequestAuthorization(token).ResponseAsync;
            var result = response.Result;
            this.logger.TryGet()?.Log($"RequestAuthorization: {result}");

            if (result == NetResult.Success)
            {
                result = await service.Restart();
                this.logger.TryGet()?.Log($"Restart: {result}");
            }
        }
    }

    private ILogger logger;
    private Terminal terminal;
    private AuthorityVault authorityVault;
}

public record RemoteSubcommandRestartOptions
{
    [SimpleOption("authority", Description = "Authority", Required = true)]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;
}
