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
            ShowErrorMessage();
            return;
        }

        var st = StringHelper.SerializeToString(creditIdentity);
        this.userInterfaceService.WriteLine($"CreditIdentity: {st}"); // creditIdentity.ToString()

        var credit = creditIdentity.ToCredit();
        if (credit is not null)
        {
            this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
        }

        void ShowErrorMessage()
        {
            this.userInterfaceService.WriteLine(Hashed.Subcommands.InvalidCreditIdentity);
            this.userInterfaceService.WriteLine($"{HashedString.Get(Hashed.Subcommands.Example)} {LpConstants.LpIdentity.ToString()}");
        }
    }
}
