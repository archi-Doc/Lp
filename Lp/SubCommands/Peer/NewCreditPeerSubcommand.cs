// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new-credit-peer")]
public class NewCreditPeerSubcommand : ISimpleCommandAsync
{// Control -> context.AddSubcommand(typeof(Lp.Subcommands.SetCreditPeerSubcommand));
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;

    public NewCreditPeerSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(string[] args)
    {
        var codeAndCredit = args.JoinWithSpace();
        this.bigMachine.DomainMachine.CreateAlways(codeAndCredit);
    }
}
