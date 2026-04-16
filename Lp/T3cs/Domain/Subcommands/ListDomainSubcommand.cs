// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.T3cs.Domain;

[SimpleCommand("list-domain")]
public class ListDomainSubcommand : ISimpleCommand
{
    // private readonly IUserInterfaceService userInterfaceService;
    private readonly DomainControl domainControl;

    public ListDomainSubcommand(DomainControl domainControl)
    {
        this.domainControl = domainControl;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.domainControl.ListDomain();
    }
}
