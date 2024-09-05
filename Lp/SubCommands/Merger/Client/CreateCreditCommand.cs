// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerClient;

[SimpleCommand("create-credit")]
public class CreateCreditCommand : ISimpleCommandAsync<CreateCreditOptions>
{
    public CreateCreditCommand(ILogger<CreateCreditCommand> logger, NetTerminal netTerminal, NestedCommand nestedcommand, AuthorityVault authorityVault, RobustConnection.Terminal robustConnectionTerminal)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
        this.nestedcommand = nestedcommand;
        this.authorityVault = authorityVault;
        this.robustConnectionTerminal = robustConnectionTerminal;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        var authority = await this.authorityVault.GetAuthority(options.AuthorityName);
        if (authority is null)
        {
            this.logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.AuthorityName);
            return;
        }

        this.logger.TryGet()?.Log(string.Empty);
        var robustConnection = this.robustConnectionTerminal.Open(this.nestedcommand.Node, new(x => VerificationHelper.SetAuthenticationToken(x, authority)));
        // var robustConnection = this.robustConnectionTerminal.Open(this.nestedcommand.Node, new(x => RobustConnection.SetAuthenticationToken(x, authority.UnsafeGetPrivateKey())));
        if (await robustConnection.Get() is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerClient>();

        var proof = new CreateCreditProof();
        authority.SignProof(proof, Mics.GetCorrected());
        var param = new Merger.CreateCreditParams(proof);

        var response2 = await service.CreateCredit(param).ResponseAsync;
        if (response2.IsSuccess && response2.Value is { } result2)
        {
            this.logger.TryGet()?.Log(result2.ToString());
        }
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
    private readonly NestedCommand nestedcommand;
    private readonly AuthorityVault authorityVault;
    private readonly RobustConnection.Terminal robustConnectionTerminal;
}

public record CreateCreditOptions
{
    [SimpleOption("Authority", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
