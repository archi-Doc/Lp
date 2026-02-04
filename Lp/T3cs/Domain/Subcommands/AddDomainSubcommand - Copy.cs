// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("list-domain")]
public class ListDomainSubcommand : ISimpleCommandAsync
{
    private readonly IUserInterfaceService userInterfaceService;
    private readonly DomainControl domainControl;

    public ListDomainSubcommand(IUserInterfaceService userInterfaceService, DomainControl domainControl, SimpleParser parser)
    {
        this.userInterfaceService = userInterfaceService;
        this.domainControl = domainControl;
    }

    public async Task RunAsync(string[] args)
    {
        this.domainControl.ListDomain();
    }
}
