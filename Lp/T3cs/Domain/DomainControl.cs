// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Net;
using Lp.Services;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(UseServiceProvider = true)]
public partial class DomainControl
{
    public const string Filename = "DomainControl";

    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly LpBase lpBase;
    private readonly NetUnit netUnit;
    private readonly AuthorityControl authorityControl;

    [Key(0)]
    private readonly ConcurrentDictionary<ulong, DomainData> domainHashToData = new();

    #endregion

    public DomainControl(ILogger<DomainControl> logger, IUserInterfaceService userInterfaceService, LpService lpService, LpBase lpBase, NetUnit netUnit, AuthorityControl authorityControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.lpBase = lpBase;
        this.netUnit = netUnit;
        this.authorityControl = authorityControl;
    }

    public void ListDomain()
    {
        var array = this.domainHashToData.Values.ToArray();
        foreach (var x in array)
        {
            this.userInterfaceService.WriteLine(x.ToString());
        }
    }

    public async Task Prepare(UnitContext unitContext)
    {
        var domain = this.lpBase.Options.AssignDomain;
        if (!string.IsNullOrEmpty(domain))
        {
            var result = await this.AddDomain(domain).ConfigureAwait(false);
            if (result != T3csResult.Success)
            {
                throw new PanicException();
            }
        }

        // var seedKey = await this.authorityControl.GetSeedKey(LpConstants.DomainKeyAlias).ConfigureAwait(false);

        // this.DomainServer.Initialize(this.PrimaryDomain, seedKey);
        this.netUnit.Services.Register<IDomainService, DomainServiceAgent>();

        // this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.ServiceEnabled, this.PrimaryDomain.DomainOption.Credit.ConvertToString(Alias.Instance));
    }

    public Task<T3csResult> AddDomain(string text, bool verbose = true)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult(T3csResult.InvalidData);
        }

        var domainAssignment = StringHelper.DeserializeFromString<DomainAssignment>(text);
        if (domainAssignment is null)
        {
            if (!verbose)
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Domain.ParseError, text);
                this.userInterfaceService.WriteLine(StringHelper.SerializeToString(Example.DomainAssignment));
            }

            return Task.FromResult(T3csResult.InvalidData);
        }

        return this.AddDomain(domainAssignment, verbose);
    }

    public async Task<T3csResult> AddDomain(DomainAssignment domainAssignment, bool verbose = true)
    {
        SeedKey? seedKey = default;
        if (!string.IsNullOrEmpty(domainAssignment.Code))
        {
            seedKey = await this.lpService.GetSeedKeyFromCode(domainAssignment.Code).ConfigureAwait(false);
            if (seedKey is null)
            {
                return T3csResult.InvalidData;
            }
        }

        var domainData = this.AddDomainInternal(domainAssignment, seedKey);
        return T3csResult.Success;
    }

    internal DomainData? GetDomainService(ulong domainHash)
    {
        if (this.domainHashToData.TryGetValue(domainHash, out var domainServiceClass))
        {
            return domainServiceClass;
        }

        return null;
    }

    internal DomainData AddDomainInternal(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        var domainHash = domainAssignment.Credit.GetDomainHash();
        var serviceClass = this.domainHashToData.AddOrUpdate(
            domainHash,
            hash =>
            {//
                return new DomainData(domainAssignment);
            },
            (hash, original) =>
            {
                original.Update(domainSeedKey);
                return original;
            });

        return serviceClass;
    }

    internal bool TryRemoveDomainService(ulong domainHash, DomainRole role)
    {
        if (role != DomainRole.Root &&
            this.domainHashToData.TryGetValue(domainHash, out var domainData))
        {
            if (domainData.Role == role)
            {
                return this.domainHashToData.TryRemove(new(domainHash, domainData));
            }
        }

        return false;
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
