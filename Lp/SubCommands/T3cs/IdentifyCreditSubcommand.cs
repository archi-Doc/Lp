// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("identify-credit", Description = "Create a Credit from CreditIdentity")]
public class IdentifyCreditSubcommand : ISimpleCommandAsync
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;

    public IdentifyCreditSubcommand(IUserInterfaceService userInterfaceService, ILogger<IdentifyCreditSubcommand> logger)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowErrorMessage();
            return;
        }

        var creditIdentity = StringHelper.DeserializeFromString<CreditIdentity>(args[0]);
        if (creditIdentity is null)
        {
            ShowErrorMessage();
            return;
        }

        var credit = creditIdentity.ToCredit();
        if (credit is null)
        {
            ShowErrorMessage();
            return;
        }

        this.logger.GetWriter()?.Write($"Credit was created successfully");
        this.logger.GetWriter()?.Write($"CreditIdentity: {StringHelper.SerializeToString(creditIdentity)}");
        this.logger.GetWriter()?.Write($"Credit: {credit.ToString()}");

        void ShowErrorMessage()
        {
            this.userInterfaceService.WriteLine(Hashed.Subcommands.InvalidCreditIdentity);
            this.userInterfaceService.WriteLine($"{HashedString.Get(Hashed.Subcommands.Example)} {Example.CreditIdentity.ToString()}");
        }
    }
}
