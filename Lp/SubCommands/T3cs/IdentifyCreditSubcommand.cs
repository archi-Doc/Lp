// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("identify-credit")]
public class IdentifyCreditSubcommand : ISimpleCommandAsync
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;

    public IdentifyCreditSubcommand(IUserInterfaceService userInterfaceService, ILogger<InspectOwnerOptions> logger, LpService lpService)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        CreditIdentity? creditIdentity = default;
        try
        {
            creditIdentity = TinyhandSerializer.DeserializeFromString<CreditIdentity>(SimpleParserHelper.TrimQuotesAndBracket(args[0]));
        }
        catch
        {
        }

        if (creditIdentity is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine($"CreditIdentity: {creditIdentity.ToString()}");

        var credit = creditIdentity.ToCredit();
        if (credit is not null)
        {
            this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
        }
    }
}
