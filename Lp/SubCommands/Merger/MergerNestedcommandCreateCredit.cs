// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("create-credit")]
public class MergerNestedcommandCreateCredit : ISimpleCommandAsync<CreateCreditOptions>
{
    public MergerNestedcommandCreateCredit(ILogger<MergerNestedcommandCreateCredit> logger, NetTerminal terminal, MergerNestedcommand nestedcommand, AuthorityVault authorityVault, AuthenticatedTerminalFactory authorizedTerminalFactory)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.nestedcommand = nestedcommand;
        this.authorityVault = authorityVault;
        this.authenticatedTerminalFactory = authorizedTerminalFactory;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        this.logger.TryGet()?.Log(string.Empty);
        using (var authenticated = await this.authenticatedTerminalFactory.Create(this.terminal, this.nestedcommand.Node, options.AuthorityName, this.logger))
        {
            if (authenticated == null)
            {
                return;
            }

            var service = authenticated.Connection.GetService<IMergerService>();

            /*var token = await authorized.Terminal.CreateToken(Token.Type.CreateCredit);
            authorized.Authority.SignToken(token);
            var param = new Merger.CreateCreditParams(token);*/

            var proof = new CreateCreditProof();
            authenticated.Authority.SignProof(proof, Mics.GetCorrected());
            var param = new Merger.CreateCreditParams(proof);

            var response2 = await service.CreateCredit(param).ResponseAsync;
            if (response2.IsSuccess && response2.Value is { } result2)
            {
                this.logger.TryGet()?.Log(result2.ToString());
            }
        }
    }

    private ILogger logger;
    private NetTerminal terminal;
    private MergerNestedcommand nestedcommand;
    private AuthorityVault authorityVault;
    private AuthenticatedTerminalFactory authenticatedTerminalFactory;
}

public record CreateCreditOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
