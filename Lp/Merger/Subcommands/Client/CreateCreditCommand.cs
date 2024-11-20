// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerClient;

[SimpleCommand("create-credit")]
public class CreateCreditCommand : ISimpleCommandAsync<CreateCreditOptions>
{
    public CreateCreditCommand(ILogger<CreateCreditCommand> logger, NetTerminal netTerminal, NestedCommand nestedcommand, AuthorityControl authorityControl, RobustConnection.Factory robustConnectionFactory)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
        this.nestedcommand = nestedcommand;
        this.authorityControl = authorityControl;
        this.robustConnectionFactory = robustConnectionFactory;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        if (this.nestedcommand.RobustConnection is not { } robustConnection ||
            this.nestedcommand.Authority is not { } authority)
        {
            return;
        }

        this.logger.TryGet()?.Log(string.Empty);
        if (await robustConnection.GetConnection(this.logger) is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerClient>();

        var proof = new CreateCreditProof();
        authority.SignProof(proof, Mics.FromDays(1));
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
    private readonly AuthorityControl authorityControl;
    private readonly RobustConnection.Factory robustConnectionFactory;
}

public record CreateCreditOptions
{
}
