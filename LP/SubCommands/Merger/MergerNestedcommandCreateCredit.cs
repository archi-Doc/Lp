// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using LP.NetServices.T3CS;
using LP.T3CS;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("create-credit")]
public class MergerNestedcommandCreateCredit : ISimpleCommandAsync<CreateCreditOptions>
{
    public MergerNestedcommandCreateCredit(ILogger<MergerNestedcommandCreateCredit> logger, Terminal terminal, MergerNestedcommand nestedcommand, Authority authority, AuthorizedTerminalFactory authorizedTerminalFactory)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.nestedcommand = nestedcommand;
        this.authority = authority;
        this.authorizedTerminalFactory = authorizedTerminalFactory;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        this.logger.TryGet()?.Log(string.Empty);
        using (var authorized = await this.authorizedTerminalFactory.Create<IMergerService>(this.terminal, this.nestedcommand.Node, options.Authority, this.logger))
        {
            if (authorized == null)
            {
                return;
            }

            var token = await authorized.Terminal.CreateToken(Token.Type.CreateCredit);
            authorized.Key.SignToken(token);
            var param = new Merger.CreateCreditParams(
                token);
            var response2 = await authorized.Service.CreateCredit(param).ResponseAsync;
            if (response2.IsSuccess && response2.Value is { } result2)
            {
                this.logger.TryGet()?.Log(result2.ToString());
            }
        }
    }

    private ILogger logger;
    private Terminal terminal;
    private MergerNestedcommand nestedcommand;
    private Authority authority;
    private AuthorizedTerminalFactory authorizedTerminalFactory;
}

public record CreateCreditOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string Authority { get; init; } = string.Empty;
}
