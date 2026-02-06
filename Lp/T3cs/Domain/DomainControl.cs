// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Lp.Net;
using Lp.Services;
using Netsphere.Crypto;
using static System.Net.Mime.MediaTypeNames;

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
    private readonly BigMachine bigMachine;

    [Key(0)]
    private readonly ConcurrentDictionary<ulong, DomainData> domainHashToData = new();

    private DomainData[]? domainDataArray;

    public DomainData[] DomainDataArray => this.domainDataArray ??= this.domainHashToData.Values.ToArray();

    #endregion

    public DomainControl(ILogger<DomainControl> logger, IUserInterfaceService userInterfaceService, LpService lpService, LpBase lpBase, NetUnit netUnit, BigMachine bigMachine)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.lpBase = lpBase;
        this.netUnit = netUnit;
        this.bigMachine = bigMachine;
    }

    public void ListDomain()
    {
        foreach (var x in this.DomainDataArray)
        {
            this.userInterfaceService.WriteLine(x.ToString());
        }
    }

    public async Task Prepare(UnitContext unitContext)
    {
        var domain = this.lpBase.Options.Domain;
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

    internal DomainData? GetDomainData(ulong domainHash)
    {
        if (this.domainHashToData.TryGetValue(domainHash, out var domainServiceClass))
        {
            return domainServiceClass;
        }

        return null;
    }

    internal DomainData AddDomainInternal(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        var domainHash = domainAssignment.GetDomainHash();
        var serviceClass = this.domainHashToData.AddOrUpdate(
            domainHash,
            hash =>
            {
                var domainData = new DomainData(domainAssignment, domainSeedKey);
                this.bigMachine.DomainMachine.GetOrCreate(domainHash);
                return domainData;
            },
            (hash, original) =>
            {
                original.Initialize(domainAssignment, domainSeedKey);
                this.bigMachine.DomainMachine.GetOrCreate(domainHash);
                return original;
            });

        this.domainDataArray = default;
        return serviceClass;
    }

    internal bool TryRemoveDomain(string domainName)
    {
        var result = false;
        foreach (var x in this.DomainDataArray)
        {
            if (string.Equals(x.DomainAssignment.Name, domainName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (this.TryRemoveDomain(x.DomainAssignment.GetDomainHash()))
                {
                    this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Domain.Removed, domainName);
                    result = true;
                }
            }
        }

        if (result)
        {
            this.domainDataArray = default;
        }

        return result;
    }

    internal bool TryRemoveDomain(ulong domainHash)
    {
        if (this.bigMachine.DomainMachine.TryGet(domainHash, out var machine))
        {
            machine.TerminateMachine();
        }

        return this.domainHashToData.TryRemove(domainHash, out _);

        /*if (role != DomainRole.Root &&
            this.domainHashToData.TryGetValue(domainHash, out var domainData))
        {
            if (domainData.Role == role)
            {
                return this.domainHashToData.TryRemove(domainHash, out _);
            }
        }

        return false;*/
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
