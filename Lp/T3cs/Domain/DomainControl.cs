// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Net;
using Lp.Services;
using Netsphere.Crypto;

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly AuthorityControl authorityControl;
    public readonly DomainService domainService;

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase, NetControl netControl, AuthorityControl authorityControl, DomainService domainData)
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
        this.domainService = domainData;
    }

    public async Task Prepare()
    {
        var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        if (this.PrimaryDomain.Initialize(seedKey, this.domainService))
        {
            this.netControl.Services.Register<IDomainService, DomainServiceAgent>();

            this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.Credit.ConvertToString());
        }
        else
        {
        }
    }

    public async Task<NetResult> RegisterNode(NodeProof nodeProof)
    {
        var domainNode = this.PrimaryDomain.NetNode;
        if (!domainNode.Validate())
        {
            //return NetResult.NoNetwork;
        }

        using (var connection = await this.netControl.NetTerminal.Connect(domainNode).ConfigureAwait(false))
        {
            if (connection is null)
            {
                return NetResult.NoNetwork;
            }

            var service = connection.GetService<IDomainService>();
            var result = await service.RegisterNode(nodeProof);
            var result2 = await service.GetNode(nodeProof.PublicKey);
            return result;
        }
    }
}
