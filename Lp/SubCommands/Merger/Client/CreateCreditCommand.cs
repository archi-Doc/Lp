// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerClient;

[SimpleCommand("create-credit")]
public class CreateCreditCommand : ISimpleCommandAsync<CreateCreditOptions>
{
    public CreateCreditCommand(ILogger<CreateCreditCommand> logger, NetTerminal terminal, NestedCommand nestedcommand, AuthorityVault authorityVault, AuthenticatedTerminalFactory authorizedTerminalFactory)
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

            var service = authenticated.Connection.GetService<IMergerClient>();

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

    private readonly ILogger logger;
    private readonly NetTerminal terminal;
    private readonly NestedCommand nestedcommand;
    private readonly AuthorityVault authorityVault;
    private readonly AuthenticatedTerminalFactory authenticatedTerminalFactory;
}

public record CreateCreditOptions
{
    [SimpleOption("Authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
