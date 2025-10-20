// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Net;
using Lp.Services;
using Netsphere.Crypto;

namespace Lp.T3cs;

public partial record class DomainControl
{
    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly NetUnit netUnit;
    private readonly AuthorityControl authorityControl;
    private readonly ConcurrentDictionary<ulong, DomainData> domainServiceDictionary = new();

    public DomainServer DomainServer { get; }

    public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(ILogger<DomainControl> logger, LpBase lpBase, NetUnit netUnit, AuthorityControl authorityControl, DomainServer domainServer)
    {
        this.logger = logger;

        var domain = lpBase.Options.Domain;
        if (!string.IsNullOrEmpty(domain))
        {
            if (DomainIdentifier.TryParse(lpBase.Options.Domain, out var domainOption, out _))
            {
                this.PrimaryDomain = new(domainOption);
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Domain.ParseError, domain);
            }
        }

        this.PrimaryDomain ??= CreditDomain.UnsafeConstructor();
        this.netUnit = netUnit;
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

        this.DomainServer.Initialize(this.PrimaryDomain, seedKey);
        this.netUnit.Services.Register<IDomainService, DomainServiceAgent>();

        // this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.DomainOption.Credit.ConvertToString(Alias.Instance));
    }

    internal DomainData? GetDomainService(ulong domainHash)
    {
        if (this.domainServiceDictionary.TryGetValue(domainHash, out var domainServiceClass))
        {
            return domainServiceClass;
        }

        return null;
    }

    internal DomainData AddDomainService(Credit domainCredit, DomainData.Role kind, SeedKey? domainSeedKey)
    {
        var domainHash = domainCredit.GetXxHash3();
        var serviceClass = this.domainServiceDictionary.AddOrUpdate(
            domainHash,
            hash =>
            {//
                var serviceClass = new DomainData(domainCredit);
                serviceClass.Update(kind, domainSeedKey);
                return serviceClass;
            },
            (hash, original) =>
            {
                original.Update(kind, domainSeedKey);
                return original;
            });

        return serviceClass;
    }

    internal bool TryRemoveDomainService(ulong domainHash)
    {
        return this.domainServiceDictionary.TryRemove(domainHash, out _);
    }

    /*public async Task<NetResult> RegisterNodeToDomain(NodeProof nodeProof)
    {
        using (var connection = await this.netUnit.NetTerminal.Connect(this.PrimaryDomain.DomainOption.NetNode).ConfigureAwait(false))
        {
            if (connection is null)
            {
                return NetResult.NoNetwork;
            }

            var service = connection.GetService<IDomainService>();
            var result = await service.RegisterNode(nodeProof);
            return result;
        }
    }*/
}
