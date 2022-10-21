// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using LP.NetServices;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands.Dump;

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
        if (!SubcommandService.TryParseNodeInformation(this.logger, options.Node, out var nodeInformation))
        {
            return;
        }

        var authorityKey = await this.authority.GetKeyAsync(options.Authority);
        if (authorityKey == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }

        using (var terminal = this.terminal.Create(nodeInformation))
        {
            var token = CallContext.Current.CreateToken(Token.Type.RequestAuthorization);
            /*await authorityInterface.SignToken(token);
            Debug.Assert(token.Veri);


            var token = new Token(Token.Type.RequestAuthorization, Mics.GetFixedUtcNow(),
            await authorityInterface.CreateToken(Credit.Default, CallContext.Current.ServerContext.Terminal.Salt, out var token);*/

            var service = terminal.GetService<IRemoteControlService>();
            /*var netResult = await service.RequestAuthorization(new Token());
            if (netResult != NetResult.Success)
            {
                netResult = await service.Restart();
            }*/
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
