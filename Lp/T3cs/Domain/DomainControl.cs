// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Net;
using Lp.Services;
using Netsphere.Crypto;

namespace Lp.T3cs;

public partial class DomainControl : UnitBase, IUnitPreparable
{
    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly LpBase lpBase;
    private readonly NetUnit netUnit;
    private readonly AuthorityControl authorityControl;
    private readonly DomainStorage domainStorage;
    private readonly ConcurrentDictionary<ulong, DomainData> domainDataDictionary = new();

    // public DomainServer DomainServer { get; }

    // public CreditDomain PrimaryDomain { get; }

    #endregion

    public DomainControl(UnitContext context, ILogger<DomainControl> logger, IUserInterfaceService userInterfaceService, LpService lpService, LpBase lpBase, NetUnit netUnit, AuthorityControl authorityControl, DomainStorage domainStorage)
        : base(context)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.lpBase = lpBase;
        this.netUnit = netUnit;
        this.authorityControl = authorityControl;
        this.domainStorage = domainStorage;
    }

    public Task<T3csResult> AssignDomain(string text)
    {
        var domainAssignment = StringHelper.DeserializeFromString<DomainAssignment>(text);
        if (domainAssignment is null)
        {
            return Task.FromResult(T3csResult.InvalidData);
        }

        return this.AssignDomain(domainAssignment);
    }

    public async Task<T3csResult> AssignDomain(DomainAssignment domainAssignment)
    {
        var code = "AB";//
        var seedKey = await this.lpService.ParseCode(this.logger, code).ConfigureAwait(false);
        if (seedKey is null)
        {
            return T3csResult.InvalidData;
        }

        var domainData = this.AddDomainService(domainAssignment.Credit, DomainRole.User, seedKey);
        return T3csResult.Success;
    }

    public async Task Prepare()
    {
        var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);
        if (seedKey is null)
        {
            return;
        }

        // this.DomainServer.Initialize(this.PrimaryDomain, seedKey);
        this.netUnit.Services.Register<IDomainService, DomainServiceAgent>();

        // this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.DomainOption.Credit.ConvertToString(Alias.Instance));
    }

    internal DomainData? GetDomainService(ulong domainHash)
    {
        if (this.domainDataDictionary.TryGetValue(domainHash, out var domainServiceClass))
        {
            return domainServiceClass;
        }

        return null;
    }

    internal DomainData AddDomainService(Credit domainCredit, DomainRole kind, SeedKey? domainSeedKey)
    {
        var domainHash = domainCredit.GetDomainHash();
        var serviceClass = this.domainDataDictionary.AddOrUpdate(
            domainHash,
            hash =>
            {//
                var domainData = new DomainData(domainCredit);
                // domainData.Update(kind, domainSeedKey);
                return domainData;
            },
            (hash, original) =>
            {
                original.Update(kind, domainSeedKey);
                return original;
            });

        return serviceClass;
    }

    internal bool TryRemoveDomainService(ulong domainHash, DomainRole role)
    {
        if (role != DomainRole.Root &&
            this.domainDataDictionary.TryGetValue(domainHash, out var domainData))
        {
            if (domainData.Role == role)
            {
                return this.domainDataDictionary.TryRemove(new(domainHash, domainData));
            }
        }

        return false;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        var domain = lpBase.Options.AssignDomain;
        if (!string.IsNullOrEmpty(domain))
        {
            var domainAssignment = StringHelper.DeserializeFromString<DomainAssignment>(domain);
            if (domainAssignment is not null)
            {
                // this.PrimaryDomain = new(domainOption);
                this.AssignDomain(domainAssignment).Wait();//
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Domain.ParseError, domain);
                var example = new DomainAssignment("Code", LpConstants.LpCredit, Alternative.NetNode);
                this.userInterfaceService.WriteLine(StringHelper.SerializeToString(example));
            }
        }
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
