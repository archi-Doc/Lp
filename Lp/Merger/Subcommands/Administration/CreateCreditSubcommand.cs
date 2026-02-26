// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("create-credit")]
public class CreateCreditSubcommand : ISimpleCommandAsync
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;

    public CreateCreditSubcommand(ILogger<CreateCreditSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(string[] args)
    {
        if (await this.nestedcommand.RobustConnection.Get(this.logger) is not { } connection)
        {
            return;
        }

        CreditIdentity? creditIdentity = default;
        try
        {
            //creditIdentity = TinyhandSerializer.DeserializeFromString<CreditIdentity>(SimpleParserHelper.TrimQuotesAndBracket(args[0]));
            creditIdentity = TinyhandSerializer.DeserializeFromString<CreditIdentity>(args[0]);
        }
        catch
        {
        }

        if (creditIdentity is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine($"CreditIdentity: {creditIdentity.ToString()}");

        var service = connection.GetService<IMergerAdministration>();
        var r = await service.CreateCredit(creditIdentity);
        this.logger.TryGet()?.Log($"{r.ToString()}");
    }
}
