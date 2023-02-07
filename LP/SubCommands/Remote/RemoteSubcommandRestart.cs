// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using LP.NetServices;
using LP.T3CS;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("restart")]
public class RemoteSubcommandRestart : ISimpleCommandAsync<RemoteSubcommandRestartOptions>
{
    public RemoteSubcommandRestart(ILogger<RemoteSubcommandRestart> logger, Terminal terminal, Authority authority)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.authority = authority;
    }

    public async Task RunAsync(RemoteSubcommandRestartOptions options, string[] args)
    {
        if (!NetHelper.TryParseNodeInformation(this.logger, options.Node, out var nodeInformation))
        {
            return;
        }

        var authorityKey = await this.authority.GetKeyAsync(options.Authority);
        if (authorityKey == null)
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

            var token = await terminal.CreateToken(Token.Type.RequestAuthorization);
            if (token == null)
            {
                return;
            }

            authorityKey.SignToken(token);
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
    private Authority authority;
}

public record RemoteSubcommandRestartOptions
{
    [SimpleOption("authority", Description = "Authority", Required = true)]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;
}
