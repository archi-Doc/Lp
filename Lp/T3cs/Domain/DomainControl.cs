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

    public DomainServer DomainServer { get; }

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase, NetControl netControl, AuthorityControl authorityControl, DomainServer domainServer)
    {
        this.logger = logger;

        var domain = lpBase.Options.Domain;
        if (!string.IsNullOrEmpty(domain))
        {
            if (DomainOption.TryParse(lpBase.Options.Domain, out var domainOption, out _))
            {
                this.PrimaryDomain = new(domainOption);
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Domain.ParseError, domain);
            }
        }

        this.PrimaryDomain ??= CreditDomain.UnsafeConstructor();
        this.netControl = netControl;
        this.authorityControl = authorityControl;
        this.DomainServer = domainServer;
    }

    public async Task Prepare()
    {
        var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        if (this.DomainServer.Initialize(this.PrimaryDomain.DomainOption.Credit, seedKey))
        {
            this.netControl.Services.Register<IDomainService, DomainServer>();

            this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.DomainOption.Credit.ConvertToString());
        }
    }

    public async Task<NetResult> RegisterNode(NodeProof nodeProof)
    {
        using (var connection = await this.netControl.NetTerminal.Connect(this.PrimaryDomain.DomainOption.NetNode).ConfigureAwait(false))
        {
            if (connection is null)
            {
                return NetResult.NoNetwork;
            }

            var service = connection.GetService<IDomainService>();
            var result = await service.RegisterNode(nodeProof);
            return result;
        }
    }
}
