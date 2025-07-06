// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase)
    {
        this.logger = logger;

        var domainOption = lpBase.Options.Domain;
        if (!string.IsNullOrEmpty(domainOption))
        {
            if (CreditDomain.TryParse(lpBase.Options.Domain, out var domain, out _))
            {
                this.PrimaryDomain = domain;
            }
            else
            {
            }
        }

        this.PrimaryDomain ??= new(Credit.Default, new(), string.Empty);
    }
}
