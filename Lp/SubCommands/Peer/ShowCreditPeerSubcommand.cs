// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("show-credit-peer")]
public class ShowCreditPeerSubcommand : ISimpleCommandAsync
{// DomainMachine: CreditPeer, RelayPeer, ContentPeer, 
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly BigMachine bigMachine;

    public ShowCreditPeerSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.bigMachine = bigMachine;
    }

    public async Task RunAsync(string[] args)
    {
        if (this.bigMachine.DomainMachine.Get() is { } interfaceInstance)
        {
            await interfaceInstance.Command.Show();
        }
    }
}
