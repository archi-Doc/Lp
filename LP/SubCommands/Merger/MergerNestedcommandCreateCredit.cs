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
    public MergerNestedcommandCreateCredit(ILogger<MergerNestedcommandCreateCredit> logger, NetTerminal terminal, MergerNestedcommand nestedcommand, AuthorityVault authorityVault, AuthorizedTerminalFactory authorizedTerminalFactory)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.nestedcommand = nestedcommand;
        this.authorityVault = authorityVault;
        this.authorizedTerminalFactory = authorizedTerminalFactory;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        this.logger.TryGet()?.Log(string.Empty);
        using (var authorized = await this.authorizedTerminalFactory.Create<IMergerService>(this.terminal, this.nestedcommand.Node, options.AuthorityName, this.logger))
        {
            if (authorized == null)
            {
                return;
            }

            var service = authorized.Connection.GetService<IMergerService>();

            /*var response = await service.GetInformation().ResponseAsync;
            if (response.IsSuccess && response.Value is { } informationResult)
            {
                this.logger.TryGet()?.Log(informationResult.Name);
            }

            return;*/

            /*var token = await authorized.Terminal.CreateToken(Token.Type.CreateCredit);
            authorized.Authority.SignToken(token);
            var param = new Merger.CreateCreditParams(token);*/

            var proof = new CreateCreditProof();
            authorized.Authority.SignProof(proof, Mics.GetCorrected());
            var param = new Merger.CreateCreditParams(proof);

            var response2 = await authorized.Service.CreateCredit(param).ResponseAsync;
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
    private AuthorizedTerminalFactory authorizedTerminalFactory;
}

public record CreateCreditOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
