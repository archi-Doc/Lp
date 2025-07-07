// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Net;
using Lp.Services;

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly AuthorityControl authorityControl;
    private readonly DomainData domainData;

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase, NetControl netControl, AuthorityControl authorityControl, DomainData domainData)
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
        this.netControl = netControl;
        this.authorityControl = authorityControl;
        this.domainData = domainData;
    }

    public async Task Prepare()
    {
        var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        if (this.PrimaryDomain.Initialize(seedKey, this.domainData))
        {
            this.netControl.Services.Register<IDomainService, DomainServiceAgent>();

            this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.Credit.ConvertToString());
        }
        else
        {
        }
    }
}
