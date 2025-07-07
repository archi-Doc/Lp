// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
    private readonly DomainData domainData;

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase, AuthorityControl authorityControl, DomainData domainData)
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
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Domain.ParseError, domainOption);
            }
        }

        this.PrimaryDomain ??= new(Credit.Default, new(), string.Empty);
        this.authorityControl = authorityControl;
        this.domainData = domainData;
    }

    public async Task Prepare()
    {//
        var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);
        seedKey = await this.authorityControl.GetLpSeedKey(this.logger).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        this.PrimaryDomain.Initialize(seedKey, this.domainData);
    }
}
