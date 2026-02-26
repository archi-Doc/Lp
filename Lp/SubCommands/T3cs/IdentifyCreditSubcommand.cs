// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("identify-credit", Description = "Create a Credit from CreditIdentity")]
public class IdentifyCreditSubcommand : ISimpleCommandAsync
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;

    public IdentifyCreditSubcommand(IUserInterfaceService userInterfaceService, ILogger<IdentifyCreditSubcommand> logger, LpService lpService)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowErrorMessage();
            return;
        }

        //var creditIdentity = TinyhandSerializer.TryDeserializeFromString<CreditIdentity>(SimpleParserHelper.TrimQuotesAndBracket(args[0]));
        var creditIdentity = TinyhandSerializer.TryDeserializeFromString<CreditIdentity>(args[0]);
        if (creditIdentity is null)
        {
            ShowErrorMessage();
            return;
        }

        var credit = creditIdentity.ToCredit();
        if (credit is not null)
        {
            this.logger.TryGet()?.Log($"Credit was created successfully");
            this.logger.TryGet()?.Log($"CreditIdentity: {StringHelper.SerializeToString(creditIdentity)}");
            this.logger.TryGet()?.Log($"Credit: {credit.ToString()}");
        }

        void ShowErrorMessage()
        {
            this.userInterfaceService.WriteLine(Hashed.Subcommands.InvalidCreditIdentity);
            this.userInterfaceService.WriteLine($"{HashedString.Get(Hashed.Subcommands.Example)} {Example.CreditIdentity.ToString()}");
        }
    }
}
