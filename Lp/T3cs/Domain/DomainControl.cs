// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase)
    {
        this.logger = logger;

        var domainOption = lpBase.Options.Domain;
        if (!string.IsNullOrEmpty(domainOption) &&
            !CreditDomain.TryParse(lpBase.Options.Domain, out var domain, out _))
        {

        }
    }
}
